using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using Webserver.EventArgs;
using Webserver.Responses;

namespace Webserver
{
    /// <summary>
    /// XML Expansion methods have to be in this form
    /// </summary>
    /// <param name="e">Access to GET or POST arguments,...</param>
    /// <param name="results">This hashtable gets converted into xml on response</param>       
    public delegate void XMLResponseMethod(RequestReceivedEventArgs e, Hashtable results);

    
    public class Server
    {
        public int _PortNumber { get; private set; }
        private Socket _ListeningSocket = null;
        private Hashtable _Responses = new Hashtable();        


        /// <summary>
        /// Creates an instance running in a seperate thread
        /// </summary>
        /// <param name="portNumber">The port to listen</param>
        public Server(int portNumber = 80, bool DhcpEnable = false)
        {
            var interf = NetworkInterface.GetAllNetworkInterfaces()[0];

            if (DhcpEnable)
            {
                interf.EnableDhcp();
                interf.RenewDhcpLease();
            }

            Debug.Print("Webserver is running on " + interf.IPAddress + " /// DHCP: " + interf.IsDhcpEnabled);

            this._PortNumber = portNumber;            
            ResponseListInitialize();

            _ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ListeningSocket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
            _ListeningSocket.Listen(4);

            var webserverThread = new Thread(WaitingForRequest);
            webserverThread.Start();
        }

        /// <summary>
        /// Waiting for client to connect.
        /// When bytes were read they get wrapped to a "Reqeust" and packed into a "RequestReceivedEventArgs"
        /// </summary>
        private void WaitingForRequest()
        {
            while (true)
            {
                try
                {
                    using (Socket clientSocket = _ListeningSocket.Accept())
                    {
                        Debug.Print("Client connected");

                        int availableBytes = 0;
                        int newAvBytes;

                        //if not all incoming bytes were received by the socket
                        do
                        {
                            newAvBytes = clientSocket.Available - availableBytes;

                            if (newAvBytes == 0)
                                break;

                            availableBytes += newAvBytes;
                            newAvBytes = 0;
                            Thread.Sleep(1);
                        } while (true);

                        Debug.Print("Final byte count: " + availableBytes);

                        //ignore requests that are too big
                        if (availableBytes < Settings.MAX_REQUESTSIZE)
                        {
                            byte[] buffer = new byte[availableBytes];
                            int readByteCount = clientSocket.Receive(buffer, availableBytes, SocketFlags.None);

                            //reqeust created, checking the response possibilities
                            using (Request tempRequest = new Request(Encoding.UTF8.GetChars(buffer)))
                            {
                                Debug.Print("Request URL=" + tempRequest.URL);
                                RequestReceivedEventArgs e = new RequestReceivedEventArgs(tempRequest, clientSocket, availableBytes);
                                HandleRequest(e);
                            }
                            Debug.Print("Request destroyed");
                        }
                        else
                        {
                        }

                        Debug.Print("Client loop finished");

                        try
                        {
                            clientSocket.Close();
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            }
        }

        /// <summary>
        /// Checks an incoming request against the possible responses
        /// </summary>
        /// <param name="e"></param>
        private void HandleRequest(RequestReceivedEventArgs e)
        {
            Debug.Print("Start checking requests");
            Response response = null;

            if (_Responses.Contains(e.Request.URL))
            {
                response = (Response)_Responses[e.Request.URL];
            }
            else
            {
                response = (Response)_Responses["FileResponse"];
            }


            if (response != null)
            {
                if (response.ConditionsCheckAndDataFill(e))
                {
                    if (!response.SendResponse(e))
                        Debug.Print("Sending response failed");

                    //Thread ledThread = new Thread(new ThreadStart(delegate()
                    //{
                    //    for (int i = 0; i < 3; i++)
                    //    {
                    //        _OnboardLed.Write(true); Thread.Sleep(5);
                    //        _OnboardLed.Write(false); Thread.Sleep(20);
                    //    }
                    //}));
                    //ledThread.Start();
                }
            }

            Debug.Print("Request handling finnished");
        }

        //-------------------------------------------------------------
        //-------------------------------------------------------------
        //---------------Webserver expansion---------------------------
        //-------------------------------------------------------------
        //-------------------------------------------------------------
        //-------------------Basic methods-----------------------------

        /// <summary>
        /// Adds a Response
        /// </summary>
        /// <param name="response">XMLResponse that has to be added</param>
        public void AddResponse(Response response)
        {
            if (!_Responses.Contains(response.URL))
            {
                _Responses.Add(response.URL, response);
            }
        }

        /// <summary>
        /// Removes a Response
        /// </summary>
        /// <param name="ResponseName">XMLResponse that has to be deleted</param>
        public void RemoveResponse(String ResponseName)
        {
            if (_Responses.Contains(ResponseName))
            {
                _Responses.Remove(ResponseName);
            }
        }

        //-------------------------------------------------------------
        //-------------------------------------------------------------
        //-----------------------EXPAND this methods-------------------

        /// <summary>
        /// Initialize the basic functionalities of the webserver
        /// </summary>
        private void ResponseListInitialize()
        {
            AddResponse(new FileResponse());
            AddResponse(new XMLResponse("echo", new XMLResponseMethod(Echo)));
            AddResponse(new XMLResponse("switchDigitalPin", new XMLResponseMethod(SwitchDigitalPin)));
            AddResponse(new XMLResponse("setDigitalPin", new XMLResponseMethod(SetDigitalPin)));
            AddResponse(new XMLResponse("xmlResponselist", new XMLResponseMethod(ResponseListXML)));
            //AddResponse(new JSONResponse("jsonResponselist", new JSONResponseMethod(ResponseListJSON)));
            AddResponse(new XMLResponse("pwm", new XMLResponseMethod(SetPWM)));
            AddResponse(new XMLResponse("getAnalogPinValue", new XMLResponseMethod(GetAnalogPinValue)));
            AddResponse(new XMLResponse("getDigitalPinState", new XMLResponseMethod(GetDigitalPinState)));
            AddResponse(new XMLResponse("multixml", new XMLResponseMethod(MultipleXML)));
            //AddResponse(new IndexResponse());
        }

        //-------------------------------------------------------------
        //---------------------Expansion Methods-----------------------
        //-------------------------------------------------------------
        //----------Look at the echo method for xml example------------

        /// <summary>
        /// Example for webserver expand method
        /// Call via http://servername/echo?value='echovalue'
        /// Submit a 'value' GET parameter
        /// </summary>
        /// <param name="e"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private void Echo(RequestReceivedEventArgs e, Hashtable results)
        {
            if (e.Request.GetArguments.Contains("value") == true)
                results.Add("echo", e.Request.GetArguments["value"]);
            else
                results.Add("ERROR", "No 'value'-parameter transmitted to server");
        }

        /// <summary>
        /// Submit a 'pin' GET parameter to switch an OutputPorts state (on/off)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private static void SwitchDigitalPin(RequestReceivedEventArgs e, Hashtable h)
        {
            if (e.Request.GetArguments.Contains("pin"))
                try
                {
                    int pin = Int32.Parse(e.Request.GetArguments["pin"].ToString());
                    if (pin >= 0 && pin <= 13)
                    {
                        PinManagement.SwitchDigitalPinState(pin);
                        h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
                    }
                }
                catch
                {
                    h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                }
            else
                h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterMissing);
        }

        /// <summary>
        /// Submit a 'pin' (0-13) and a 'state' (true/false) GET parameter to turn on/off OutputPort
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private static void SetDigitalPin(RequestReceivedEventArgs e, Hashtable h)
        {
            if (e.Request.GetArguments.Contains("pin"))
                if (e.Request.GetArguments.Contains("state"))
                    try
                    {
                        int pin = Int32.Parse(e.Request.GetArguments["pin"].ToString());
                        if (pin >= 0 && pin <= 13)
                        {
                            try
                            {
                                bool state = (e.Request.GetArguments["state"].ToString() == "true") ? true : false;
                                PinManagement.SetDigitalPinState(pin, state);
                                h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
                            }
                            catch
                            {
                                h = XMLResponse.GenerateErrorHashtable("state", ResponseErrorType.ParameterRangeException);
                            }
                        }
                        else
                            h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterRangeException);
                    }
                    catch
                    {
                        h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                    }
                else
                    h = XMLResponse.GenerateErrorHashtable("state", ResponseErrorType.ParameterMissing);
            else
                h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterMissing);
        }

        /// <summary>
        /// Returns the responses added to the webserver
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void ResponseListXML(RequestReceivedEventArgs e, Hashtable h)
        {
            foreach (Object k in _Responses.Keys)
            {
                if (_Responses[k] as XMLResponse != null)
                {
                    h.Add("methodURL", k.ToString());
                }
            }
        }

		/*
        /// <summary>
        /// Returns the responses added to the webserver
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void ResponseListJSON(RequestReceivedEventArgs e, JsonArray j)
        {
            JsonObject o;
            foreach (Object k in _Responses.Keys)
            {
                if (_Responses[k] as JSONResponse != null)
                {
                    o = new JsonObject();
                    o.Add("methodURL", k);
                    o.Add("methodInternalName", ((Response)_Responses[k]).URL);
                    j.Add(o);
                }
            }
        }
		*/


        /// <summary>
        /// Submit a 'pin' (5,6,9,10), a period and a duration (0 for off, period-value for 100% on) GET parameter to control PWM
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void SetPWM(RequestReceivedEventArgs e, Hashtable h)
        {
            if (e.Request.GetArguments.Contains("pin"))
            {
                if (e.Request.GetArguments.Contains("period"))
                {
                    if (e.Request.GetArguments.Contains("duration"))
                    {
                        try
                        {
                            int pin = Int32.Parse(e.Request.GetArguments["pin"].ToString());
                            try
                            {
                                uint duration = UInt32.Parse(e.Request.GetArguments["duration"].ToString());
                                try
                                {
                                    uint period = UInt32.Parse(e.Request.GetArguments["period"].ToString());
                                    if (PinManagement.SetPWM(pin, period, duration))
                                        h.Add("success", period + "/" + duration);
                                    else
                                        h = XMLResponse.GenerateErrorHashtable("PWM", ResponseErrorType.InternalValueNotSet);
                                }
                                catch (Exception ex)
                                {
                                    h = XMLResponse.GenerateErrorHashtable("period", ResponseErrorType.ParameterConvertError);
                                    Debug.Print(ex.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                h = XMLResponse.GenerateErrorHashtable("duration", ResponseErrorType.ParameterConvertError);
                                Debug.Print(ex.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                            Debug.Print(ex.ToString());
                        }
                    }
                    else
                        h = XMLResponse.GenerateErrorHashtable("duration", ResponseErrorType.ParameterMissing);
                }
                else
                    h = XMLResponse.GenerateErrorHashtable("period", ResponseErrorType.ParameterMissing);
            }
            else
                h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterMissing);
        }

        /// <summary>
        /// Submit a 'pin' (0-13) GET parameter. Returns true or false
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void GetDigitalPinState(RequestReceivedEventArgs e, Hashtable h)
        {
            if (e.Request.GetArguments.Contains("pin"))
            {
                try
                {
                    int pin = Int32.Parse(e.Request.GetArguments["pin"].ToString());
                    h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
                }
                catch (Exception ex)
                {
                    h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                    Debug.Print(ex.ToString());
                }
            }
            else
                h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterMissing);
        }

        /// <summary>
        /// Submit a 'pin' (0-5) GET parameter. Returns true or false
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void GetAnalogPinValue(RequestReceivedEventArgs e, Hashtable h)
        {
            if (e.Request.GetArguments.Contains("pin"))
            {
                try
                {
                    int pin = Int32.Parse(e.Request.GetArguments["pin"].ToString());
                    h.Add("pin" + pin, PinManagement.GetAnalogPinValue(pin));
                }
                catch (Exception ex)
                {
                    h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                    Debug.Print(ex.ToString());
                }
            }
            else
                h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterMissing);
        }

        /// <summary>
        /// Example for the useage of the new XML library
        /// Use the hashtable if you don't need nested XML (like the standard xml responses)
        /// If you need nested XML, use the XMLPair class. The Key-parameter is String.
        /// As value the following types can be used to achieve nesting: XMLPair, XMLPair[] and Hashtable
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void MultipleXML(RequestReceivedEventArgs e, Hashtable returnHashtable)
        {
            returnHashtable.Add("UseTheHashtable", "If you don't need nested XML");

            XMLPair[] Phones = new XMLPair[2];
            Phones[0] = new XMLPair("Phone");
            Phones[1] = new XMLPair("Phone");

            XMLPair[] PhoneAttributes0 = new XMLPair[2];
            PhoneAttributes0[0] = new XMLPair("Type", "Nokia");
            PhoneAttributes0[1] = new XMLPair("AvailableColours", new XMLPair[] {new XMLPair("Colour","Cyan"), new XMLPair("Colour","Black")});
            Phones[0].Value = PhoneAttributes0;

            XMLPair[] PhoneAttributes1 = new XMLPair[2];
            PhoneAttributes1[0] = new XMLPair("Type", "HTC");
            PhoneAttributes1[1] = new XMLPair("AvailableColours", new XMLPair("Colour", "Grey"));
            Phones[1].Value = PhoneAttributes0;

            returnHashtable.Add("Phones", Phones);
        }
    }
}

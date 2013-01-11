using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FastloadMedia.NETMF.Http;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using NeonMika.Webserver.Responses;
using NeonMika.XML;
using NeonMika.Util;
using NeonMika.Webserver.Responses.ComplexResponses;
using NeonMika.Webserver.POST;
using System.IO;

namespace NeonMika.Webserver
{
    /// <summary>
    /// XML Expansion methods have to be in this form
    /// </summary>
    /// <param name="e">Access to GET or POST arguments,...</param>
    /// <param name="results">This hashtable gets converted into xml on response</param>       
    public delegate void XMLResponseMethod(Request e, Hashtable results);

    /// <summary>
    /// JSON Expansion methods have to be in this form
    /// </summary>
    /// <param name="e">Access to GET or POST arguments,...</param>
    /// <param name="results">This JsonArray gets converted into JSON on response</param>
    /// <returns>True if URL refers to this method, otherwise false (false = SendRequest should not be executed) </returns>        
    public delegate void JSONResponseMethod(Request e, JsonArray results);

    public delegate void POSTOperationMethod(Request e, PostFileReader access);

    /// <summary>
    /// Main class of NeonMika.Webserver
    /// </summary>
    public class Server
    {
        public int _PortNumber { get; private set; }
        private Socket _ListeningSocket = null;
        private Hashtable _Responses = new Hashtable( );

        /// <summary>
        /// Creates an NeonMika.Webserver instance running in a seperate thread
        /// </summary>
        /// <param name="portNumber">The port to listen for incoming requests</param>
        public Server(int portNumber = 80)
        {
            var interf = NetworkInterface.GetAllNetworkInterfaces( )[0];

            Debug.Print("Webserver is running on " + interf.IPAddress + " /// DHCP: " + interf.IsDhcpEnabled);

            this._PortNumber = portNumber;

            ResponseListInitialize( );

            _ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ListeningSocket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
            _ListeningSocket.Listen(4);

            var webserverThread = new Thread(WaitingForRequest);
            webserverThread.Start( );
        }

        /// <summary>
        /// Waiting for client to connect.
        /// When bytes were read they get wrapped to a "Reqeust"
        /// </summary>
        private void WaitingForRequest( )
        {
            while ( true )
            {
                try
                {
                    using ( Socket clientSocket = _ListeningSocket.Accept( ) )
                    {
                        int availableBytes = 0;
                        int LoopCount = 0;
                        //if not all incoming bytes were received by the socket
                        do
                        {
                            if (availableBytes < clientSocket.Available)
                            {
                                availableBytes = clientSocket.Available;
                                LoopCount = 0;
                            }
                            else
                                LoopCount += 1;
                            Thread.Sleep(5);
                        } while (availableBytes == 0 || LoopCount < 20);

                        if ( availableBytes > 0 )
                        {
                            byte[] buffer = new byte[availableBytes > Settings.MAX_REQUESTSIZE ? Settings.MAX_REQUESTSIZE : availableBytes];
                            byte[] header = new byte[0];

                            int readByteCount = clientSocket.Receive(buffer, buffer.Length, SocketFlags.None);

                            for ( int headerend = 0; headerend < buffer.Length - 3; headerend++ )
                            {
                                if ( buffer[headerend] == '\r' && buffer[headerend + 1] == '\n' && buffer[headerend + 2] == '\r' && buffer[headerend + 3] == '\n' )
                                {
                                    header = new byte[headerend + 4];
                                    Array.Copy(buffer, 0, header, 0, headerend + 4);
                                    break;
                                }
                            }

                            //reqeust created, checking the response possibilities
                            using ( Request tempRequest = new Request(Encoding.UTF8.GetChars(header), clientSocket) )
                            {
                                Debug.Print("... Client connected ... URL: " + tempRequest.URL + " ... Final byte count: " + availableBytes);

                                if ( tempRequest.Method == "POST" )
                                {
                                    //POST was incoming, it will be saved to SD card at Settings.POST_TEMP_PATH
                                    PostToSdWriter post = new PostToSdWriter(tempRequest, buffer, header.Length);
                                    post.Receive( );
                                }

                                //Let's check if we have to take some action or if it is a file-response 
                                HandleGETResponses(tempRequest);
                            }

                            Debug.Print("Client loop finished");

                            try
                            {
                                //Close client, otherwise the browser / client won't work properly
                                clientSocket.Close( );
                            }
                            catch ( Exception ex )
                            {
                                Debug.Print(ex.ToString( ));
                            }
                        }
                    }
                }
                catch ( Exception ex )
                {
                    Debug.Print(ex.Message);
                }
            }
        }

        /// <summary>
        /// Checks what Response has to be executed.
        /// It compares the requested page URL with the URL set for the coded responses 
        /// </summary>
        /// <param name="e"></param>
        private void HandleGETResponses(Request e)
        {
            Response response = null;


            if ( _Responses.Contains(e.URL) )
                response = (Response)_Responses[e.URL];
            else
                response = (Response)_Responses["FileResponse"];


            if ( response != null )
                using ( response )
                    if ( response.ConditionsCheckAndDataFill(e) )
                    {
                        if ( !response.SendResponse(e) )
                            Debug.Print("Sending response failed");
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
            if ( !_Responses.Contains(response.URL) )
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
            if ( _Responses.Contains(ResponseName) )
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
        private void ResponseListInitialize( )
        {
            AddResponse(new FileResponse());
            AddResponse(new XMLResponse("echo", new XMLResponseMethod(Echo)));
            //AddResponse(new XMLResponse("switchDigitalPin", new XMLResponseMethod(SwitchDigitalPin)));
            //AddResponse(new XMLResponse("setDigitalPin", new XMLResponseMethod(SetDigitalPin)));
            //AddResponse(new XMLResponse("xmlResponselist", new XMLResponseMethod(ResponseListXML)));
            //AddResponse(new JSONResponse("jsonResponselist", new JSONResponseMethod(ResponseListJSON)));
            //AddResponse(new XMLResponse("pwm", new XMLResponseMethod(SetPWM)));
            //AddResponse(new XMLResponse("getAnalogPinValue", new XMLResponseMethod(GetAnalogPinValue)));
            //AddResponse(new XMLResponse("getDigitalPinState", new XMLResponseMethod(GetDigitalPinState)));
            //AddResponse(new XMLResponse("multixml", new XMLResponseMethod(MultipleXML)));
            //AddResponse(new IndexResponse(""));
            //AddResponse(new XMLResponse("upload", new XMLResponseMethod(Upload)));
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
        private void Echo(Request e, Hashtable results)
        {
            if ( e.GetArguments.Contains("value") == true )
                results.Add("echo", e.GetArguments["value"]);
            else
                results.Add("ERROR", "No 'value'-parameter transmitted to server");
        }

        /// <summary>
        /// Submit a 'pin' GET parameter to switch an OutputPorts state (on/off)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private static void SwitchDigitalPin(Request e, Hashtable h)
        {
            if ( e.GetArguments.Contains("pin") )
                try
                {
                    int pin = Int32.Parse(e.GetArguments["pin"].ToString( ));
                    if ( pin >= 0 && pin <= 13 )
                    {
                        //PinManagement.SwitchDigitalPinState(pin);
                        //h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
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
        private static void SetDigitalPin(Request e, Hashtable h)
        {
            if ( e.GetArguments.Contains("pin") )
                if ( e.GetArguments.Contains("state") )
                    try
                    {
                        int pin = Int32.Parse(e.GetArguments["pin"].ToString( ));
                        if ( pin >= 0 && pin <= 13 )
                        {
                            try
                            {
                                //bool state = ( e.GetArguments["state"].ToString( ) == "true" ) ? true : false;
                                //PinManagement.SetDigitalPinState(pin, state);
                                //h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
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
        private void ResponseListXML(Request e, Hashtable h)
        {
            foreach ( Object k in _Responses.Keys )
            {
                if ( _Responses[k] as XMLResponse != null )
                {
                    h.Add("methodURL", k.ToString( ));
                }
            }
        }

        /// <summary>
        /// Returns the responses added to the webserver
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void ResponseListJSON(Request e, JsonArray j)
        {
            JsonObject o;
            foreach ( Object k in _Responses.Keys )
            {
                if ( _Responses[k] as JSONResponse != null )
                {
                    o = new JsonObject( );
                    o.Add("methodURL", k);
                    o.Add("methodInternalName", ( (Response)_Responses[k] ).URL);
                    j.Add(o);
                }
            }
        }

        /// <summary>
        /// Submit a 'pin' (5,6,9,10), a period and a duration (0 for off, period-value for 100% on) GET parameter to control PWM
        /// </summary>
        /// <param name="e"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private void SetPWM(Request e, Hashtable h)
        {
            if ( e.GetArguments.Contains("pin") )
            {
                if ( e.GetArguments.Contains("period") )
                {
                    if ( e.GetArguments.Contains("duration") )
                    {
                        try
                        {
                            int pin = Int32.Parse(e.GetArguments["pin"].ToString( ));
                            try
                            {
                                uint duration = UInt32.Parse(e.GetArguments["duration"].ToString( ));
                                try
                                {
                                    //uint period = UInt32.Parse(e.GetArguments["period"].ToString( ));
                                    //if ( PinManagement.SetPWM(pin, period, duration) )
                                    //    h.Add("success", period + "/" + duration);
                                    //else
                                    //    h = XMLResponse.GenerateErrorHashtable("PWM", ResponseErrorType.InternalValueNotSet);
                                }
                                catch ( Exception ex )
                                {
                                    h = XMLResponse.GenerateErrorHashtable("period", ResponseErrorType.ParameterConvertError);
                                    Debug.Print(ex.ToString( ));
                                }
                            }
                            catch ( Exception ex )
                            {
                                h = XMLResponse.GenerateErrorHashtable("duration", ResponseErrorType.ParameterConvertError);
                                Debug.Print(ex.ToString( ));
                            }
                        }
                        catch ( Exception ex )
                        {
                            h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                            Debug.Print(ex.ToString( ));
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
        private void GetDigitalPinState(Request e, Hashtable h)
        {
            if ( e.GetArguments.Contains("pin") )
            {
                try
                {
                    //int pin = Int32.Parse(e.GetArguments["pin"].ToString( ));
                    //h.Add("pin" + pin, PinManagement.GetDigitalPinState(pin) ? "1" : "0");
                }
                catch ( Exception ex )
                {
                    h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                    Debug.Print(ex.ToString( ));
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
        private void GetAnalogPinValue(Request e, Hashtable h)
        {
            if ( e.GetArguments.Contains("pin") )
            {
                try
                {
                    //int pin = Int32.Parse(e.GetArguments["pin"].ToString( ));
                    //h.Add("pin" + pin, PinManagement.GetAnalogPinValue(pin));
                }
                catch ( Exception ex )
                {
                    h = XMLResponse.GenerateErrorHashtable("pin", ResponseErrorType.ParameterConvertError);
                    Debug.Print(ex.ToString( ));
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
        private void MultipleXML(Request e, Hashtable returnHashtable)
        {
            returnHashtable.Add("UseTheHashtable", "If you don't need nested XML");

            XMLList Phones = new XMLList("Phones");
            Phones.Attributes.Add("ExampleAttribute1", "NeonMika");
            Phones.Attributes.Add("ExampleAttribute2", 1992);
            XMLList BluePhones = new XMLList("BluePhones");
            XMLList BlackPhones = new XMLList("BlackPhones");
            XMLList MokiaRumia = new XMLList("Phone");
            XMLList LangsumTalaxy = new XMLList("Phone");
            MokiaRumia.Add(new XMLPair("Name", "Mokia Rumia"));
            MokiaRumia.Add(new XMLPair("PhoneNumber", 436603541897));
            XMLList WirelessConnections = new XMLList("WirelessConnections");
            WirelessConnections.Add(new XMLPair("WLAN", true));
            WirelessConnections.Add(new XMLPair("Bluetooth", false));
            MokiaRumia.Add(WirelessConnections);
            WirelessConnections.Clear( );
            WirelessConnections.Add(new XMLPair("WLAN", false));
            WirelessConnections.Add(new XMLPair("Bluetooth", true));
            LangsumTalaxy.Add(new XMLPair("Name", "Langsum Talaxy"));
            LangsumTalaxy.Add(new XMLPair("PhoneNumber", 436603541122));
            LangsumTalaxy.Add(WirelessConnections);

            Phones.Add(MokiaRumia);
            Phones.Add(LangsumTalaxy);

            returnHashtable.Add("Phones", Phones);
        }

        private void Upload(Request e, Hashtable ret)
        {
            if ( e.GetArguments.Contains("path") )
            {
                try
                {
                    string path = e.GetArguments["path"].ToString( );
                    path.Replace('/', '\\');

                    try
                    {
                        string dir = path.Substring(0, path.LastIndexOf("\\"));
                        Directory.CreateDirectory(dir);
                    }
                    catch ( Exception ex )
                    {
                        Debug.Print(ex.ToString( ));
                    }

                    try
                    {
                        FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                        Debug.Print(Debug.GC(true).ToString( ));
                        Debug.Print(Debug.GC(true).ToString( ));
                        PostFileReader post = new PostFileReader( );
                        Debug.Print(Debug.GC(true).ToString( ));
                        Debug.Print(Debug.GC(true).ToString( ));
                        for ( int i = 0; i < post.Length / Settings.FILE_BUFFERSIZE; i++ )
                            fs.Write(post.Read(Settings.FILE_BUFFERSIZE), 0, Settings.FILE_BUFFERSIZE);
                        fs.Write(post.Read((int)( post.Length % Settings.FILE_BUFFERSIZE )), 0, (int)( post.Length % Settings.FILE_BUFFERSIZE ));
                        fs.Flush( );
                        fs.Close( );
                        post.Close( );
                    }
                    catch ( Exception ex )
                    {
                        Debug.Print(ex.ToString( ));
                        ret = XMLResponse.GenerateErrorHashtable("file access", ResponseErrorType.InternalOperationError);
                    }

                }
                catch ( Exception ex ) { Debug.Print(ex.ToString( )); }
            }
            else
                ret = XMLResponse.GenerateErrorHashtable("path", ResponseErrorType.ParameterMissing);
        }
    }
}

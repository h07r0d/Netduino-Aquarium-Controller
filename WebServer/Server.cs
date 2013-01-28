using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using Webserver.Responses;
using Webserver.POST;
using Extensions;
using System.IO;

namespace Webserver
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
    //public delegate void JSONResponseMethod(Request e, JsonArray results);

    public delegate void POSTOperationMethod(Request e, PostFileReader access);

    /// <summary>
    /// Main class of NeonMika.Webserver
    /// </summary>
    public class Server
    {
        public int _PortNumber { get; private set; }
        private Socket _ListeningSocket = null;
        private Hashtable _Responses = new Hashtable( );
		private Thread _webserverThread;
        

        /// <summary>
        /// Creates an NeonMika.Webserver instance
        /// </summary>
        /// <param name="portNumber">The port to listen for incoming requests</param>
        public Server(int portNumber = 80, bool DhcpEnable = true, string ipAddress = "", string subnetMask = "", string gatewayAddress = "")
        {
            var interf = NetworkInterface.GetAllNetworkInterfaces( )[0];

            if ( DhcpEnable )
            {
                //Dynamic IP
                interf.EnableDhcp( );
                //interf.RenewDhcpLease( );
            }
            else
            {
                //Static IP
                interf.EnableStaticIP(ipAddress, subnetMask, gatewayAddress);
            }

            Debug.Print("Webserver is running on " + interf.IPAddress + " /// DHCP: " + interf.IsDhcpEnabled);

            this._PortNumber = portNumber;
                      
            ResponseListInitialize( );

            _ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ListeningSocket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
            _ListeningSocket.Listen(4);

        }

		/// <summary>
		/// Start the webserver thread
		/// </summary>
		public void Start()
		{
			_webserverThread = new Thread(WaitingForRequest);
			_webserverThread.Start(); 
		}

		/// <summary>
		/// Stop the webserver thread
		/// </summary>
		public void Stop()
		{
			_webserverThread.Abort();
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
						Debug.Print("Client Connected");
                        int availableBytes = 0;						
						int newAvBytes = 0;
						Thread.Sleep(100);
						//if not all incoming bytes were received by the socket
						/*do
						{
							if (availableBytes < clientSocket.Available)
							{
								availableBytes = clientSocket.Available;
								LoopCount = 0;
							}
							else
								LoopCount += 1;
							Thread.Sleep(1);
						} while (availableBytes == 0 || LoopCount < 300);
						*/
						do
						{
							newAvBytes = clientSocket.Available - availableBytes;

							if (newAvBytes == 0)
								break;

							availableBytes += newAvBytes;
							newAvBytes = 0;
							Thread.Sleep(1);
						} while (true);

						Debug.Print("Available Bytes: " + availableBytes);

                        if ( availableBytes > 0 )
                        {
                            byte[] buffer = new byte[availableBytes > Settings.MAX_REQUESTSIZE ? Settings.MAX_REQUESTSIZE : availableBytes];
                            byte[] header = new byte[0];

                            int readByteCount = clientSocket.Receive(buffer, buffer.Length, SocketFlags.None);
							Debug.Print(readByteCount + " bytes read");

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
            //AddResponse(new XMLResponse("echo", new XMLResponseMethod(Echo)));
            //AddResponse(new XMLResponse("switchDigitalPin", new XMLResponseMethod(SwitchDigitalPin)));
            //AddResponse(new XMLResponse("setDigitalPin", new XMLResponseMethod(SetDigitalPin)));
            //AddResponse(new XMLResponse("xmlResponselist", new XMLResponseMethod(ResponseListXML)));
            //AddResponse(new JSONResponse("jsonResponselist", new JSONResponseMethod(ResponseListJSON)));
            //AddResponse(new XMLResponse("pwm", new XMLResponseMethod(SetPWM)));
            //AddResponse(new XMLResponse("getAnalogPinValue", new XMLResponseMethod(GetAnalogPinValue)));
            //AddResponse(new XMLResponse("getDigitalPinState", new XMLResponseMethod(GetDigitalPinState)));
            //AddResponse(new XMLResponse("multixml", new XMLResponseMethod(MultipleXML)));
            //AddResponse(new IndexResponse(""));
            AddResponse(new XMLResponse("upload", new XMLResponseMethod(Upload)));
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

        

        

        

        

        

        private void Upload(Request e, Hashtable ret)
        {
            if ( e.GetArguments.Contains("path") )
            {
                try
                {
                    string path = e.GetArguments["path"].ToString( );
                    path.Replace("/", "\\");

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

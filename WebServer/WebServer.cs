using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;


namespace WebServer
{
    enum SupportedErrors { FileNotFound = 404, ServerError = 500 };
    public class WebServer : IDisposable
    {        
        private Thread serverThread = null;        
        private WebResponseThreadList _ProcessRequestWorkerList = new WebResponseThreadList();
        private int _Port = -1;
        int _ListeningID = -1;// -1 signifies not listening
        bool _Stopping = false;        

        #region Constructors

        /// <summary>
        /// Instantiates a new webserver.
        /// </summary>
        /// <param name="port">Port number to listen on.</param>
        public WebServer(int port)
        {
            _Port = port;
            Debug.Print("WebControl started on port " + port.ToString());
        }

        #endregion


        /// <summary>
        /// CommandReceived event is triggered when a valid command (plus parameters) is received.
        /// Valid commands are defined in the AllowedCommands property.
        /// </summary>
        public event ResponseHandler ResponseHandler;


        #region Public and private methods

        /// <summary>
        /// Start the server thread.
        /// </summary>
        public void Start()
        {
            // start server
            this.serverThread = new Thread(RunServer);
            serverThread.Start();
            Debug.Print("Started server in thread " + serverThread.GetHashCode().ToString());
        }

        /// <summary>
        /// Allows an external process to stop the web server completely.
        /// </summary>
        public void Stop()
        {
            _Stopping = true;
            Debug.Print("Shutting down server.");
            //bring down the thread.  
            //We have threads within threads (i.e. socket accept), so an elegant solution is a bigger change.
            this.serverThread.Abort();
        }

        /// <summary>
        /// Runs the server in its own Thread.
        /// </summary>
        private void RunServer()
        {
//            Debug.EnableGCMessages(true);// debug only
            using (Socket nonBlockingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                nonBlockingSocket.Bind(new IPEndPoint(IPAddress.Any, _Port));
                nonBlockingSocket.Listen(1);

                while (!_Stopping)
                {
                    if (_ListeningID == -1)
                    {
                        try
                        {
                            //_CurrentlyListening = true;
                            WebResponseThread currentProcessRequest = new WebResponseThread(ResponseHandler, WorkFinishedHandler, SocketAcceptedHandler);
                            while (_ListeningID < 0)
                            {
                                _ListeningID = _ProcessRequestWorkerList.Enqueue(currentProcessRequest);
                                if (_ListeningID < 0)
                                {
                                    Debug.Print("no spare thread");
                                }
                                Thread.Sleep(50);
                            }
                            currentProcessRequest.Start(nonBlockingSocket);
                            
                        }
                        catch (Exception ex)
                        {// probably lost connection
                            Debug.Print("Exception caught in Run Server: " + ex.Message);
                            _ProcessRequestWorkerList.RemoveAt(_ListeningID);
                            _ListeningID = -1;// what else to do if 
                            Thread.Sleep(200);//
                        }
                    }
                }
            }
        }

        private void WorkFinishedHandler(Exception ex, WebResponseThread requestWorker)// todo make thread safe
        {
            if (ex != null)
            {
                Debug.Print("WorkFinishedHandler exception: " + ex.Message);
            }
            _ProcessRequestWorkerList.RemoveAt(requestWorker.WorkerID);
        }

        private void SocketAcceptedHandler(WebResponseThread requestWorker)
        {
            //_CurrentlyListening = false;
            if (_ListeningID != requestWorker.WorkerID)
            {
                _ListeningID = -1;
                throw (new Exception("_ListeningID != requestWorker.WorkerID"));
            }
            _ListeningID = -1;
        }

        //private string DoResponseHandler(BaseRequest request)
        //{
        //    return ResponseHandler(request);
        //}


        public string ListInterfaces()
        {
            NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
            string s = string.Concat("Interfaces: ");
            for (int i = 1; i <= ifaces.Length; i++)
            {
                NetworkInterface iface = ifaces[i-1];
                s += string.Concat(i.ToString(), ": ip ", iface.IPAddress, " subnet ", iface.SubnetMask);
            }
            return s;
        }
        #endregion
       
        /// <summary>
        /// implement IDisposable
        /// </summary>
        public void Dispose()
        {
            _ProcessRequestWorkerList.StopAll();
            Thread.Sleep(10);
            Stop();
        }
    }
}

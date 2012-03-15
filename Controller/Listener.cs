using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace Controller
{
    public delegate void RequestReceivedDelegate(Request request);

    public class Listener : IDisposable
    {
        const int maxRequestSize = 1024;
        readonly int portNumber = 80;

        private Socket listeningSocket = null;
        private RequestReceivedDelegate requestReceived;

        public Listener(RequestReceivedDelegate RequestReceived)
            : this(RequestReceived, 80) { }

        public Listener(RequestReceivedDelegate RequestReceived, int PortNumber)
        {
            portNumber = PortNumber;
            requestReceived = RequestReceived;
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
            listeningSocket.Listen(10);

            new Thread(StartListening).Start();

        }

        ~Listener()
        {
            Dispose();
        }

        public void StartListening()
        {

            while (true)
            {
                using (Socket clientSocket = listeningSocket.Accept())
                {
                    IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;
                    Debug.Print("Received request from " + clientIP.ToString());
                    var x = clientSocket.RemoteEndPoint;

                    int availableBytes = clientSocket.Available;
                    Debug.Print(DateTime.Now.ToString() + " " + availableBytes.ToString() + " request bytes available");

                    int bytesReceived = (availableBytes > maxRequestSize ? maxRequestSize : availableBytes);
                    if (bytesReceived > 0)
                    {
                        byte[] buffer = new byte[bytesReceived]; // Buffer probably should be larger than this.
                        int readByteCount = clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);

                        using (Request r = new Request(clientSocket, Encoding.UTF8.GetChars(buffer)))
                        {
                            Debug.Print(DateTime.Now.ToString() + " " + r.URL);
                            if (requestReceived != null) requestReceived(r);

                        }


                    }
                }

                // I always like to have this in a continuous loop. Helps prevent lock-ups
                Thread.Sleep(10);
            }

        }


        #region IDisposable Members

        public void Dispose()
        {
            if (listeningSocket != null) listeningSocket.Close();

        }

        #endregion
    }
}

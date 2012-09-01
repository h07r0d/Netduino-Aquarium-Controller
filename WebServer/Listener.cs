using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace WebServer
{
	public delegate void RequestReceivedDelegate(Request request);

	public class Listener : IDisposable
	{
		const int MaxRequestSize = 1024;
		readonly int m_port = 80;

		private Socket m_listeningSocket = null;
		private RequestReceivedDelegate m_requestReceived;

		public Listener(RequestReceivedDelegate _requestReceived)
			: this(_requestReceived, 80) { }

		public Listener(RequestReceivedDelegate _requestReceived, int _port)
		{
			m_port = _port;
			m_requestReceived = _requestReceived;
			m_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_listeningSocket.Bind(new IPEndPoint(IPAddress.Any, m_port));
			m_listeningSocket.Listen(10);
		}

		~Listener() { Dispose(); }

		public void Dispose()
		{
			if (m_listeningSocket != null) m_listeningSocket.Close();			
		}

		public void Listen()
		{
			while (true)
			{
				using (Socket clientSocket = m_listeningSocket.Accept())
				{
					IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;
					Debug.Print("Received request from " + clientIP.ToString());					

					int availableBytes = clientSocket.Available;
					Debug.Print(DateTime.Now.ToString() + " " + availableBytes.ToString() + " request bytes available");

					int bytesReceived = (availableBytes > MaxRequestSize ? MaxRequestSize : availableBytes);
					if (bytesReceived > 0)
					{
						byte[] buffer = new byte[bytesReceived]; // Buffer probably should be larger than this.
						int readByteCount = clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);

						using (Request r = new Request(clientSocket, buffer))
						{
							Debug.Print(DateTime.Now.ToString() + " " + r.Uri);
							if (m_requestReceived != null) m_requestReceived(r);

						}
					}
				}
				// I always like to have this in a continuous loop. Helps prevent lock-ups
				Thread.Sleep(10);
			}
		}
	}
}

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Thingspeak : OutputPlugin
	{
		~Thingspeak() { Dispose(); }
		public override void Dispose() { }
		private string m_httpPost;
		private const string m_thingSpeakIP = "184.106.153.149";

		public Thingspeak() { }

		public Thingspeak(object _config)
		{
			Hashtable config = (Hashtable)_config;
			m_httpPost = "POST /update HTTP/1.1\nHost: api.thingspeak.com\nConnection: close\nX-THINGSPEAKAPIKEY: ";
			m_httpPost += config["writeapi"].ToString() + "\n";
			m_httpPost += "Content-Type: application/x-www-form-urlencoded\nContent-Length: ";			
		}

		private static Socket ConnectSocket(String server, Int32 port)
		{
			// Get server's IP address.
			IPHostEntry hostEntry = Dns.GetHostEntry(server);

			// Create socket and connect to the server's IP address and port
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint(hostEntry.AddressList[0], port));
			return socket;
		}

		public override void EventHandler(object _sender, IPluginData _data)
		{
			// build field string from _data and append to the post string
			string fieldData = "field"+(uint)_data.DataType()+"="+_data.GetValue().ToString("F");

			// add content length to post string, then data
			string postString = m_httpPost + fieldData.Length + "\n\n" + fieldData;

			//Open the Socket and post the data
			// create required networking parameters
			
			using (Socket thingSpeakSocket = ConnectSocket(m_thingSpeakIP, 80))
			{				
				Byte[] sendBytes = Encoding.UTF8.GetBytes(postString);
				thingSpeakSocket.Send(sendBytes, sendBytes.Length, 0);
				
				// wait for a response to see what happened
				Byte[] buffer = new Byte[256];
				String page = String.Empty;

				// Poll for data until 30-second timeout.  Returns true for data and connection closed.
				while (thingSpeakSocket.Poll(20 * 1000000, SelectMode.SelectRead))
				{
					// If there are 0 bytes in the buffer, then the connection is closed, or we have timed out.
					if (thingSpeakSocket.Available == 0) break;

					// Zero all bytes in the re-usable buffer.
					Array.Clear(buffer, 0, buffer.Length);

					// Read a buffer-sized HTML chunk.
					Int32 bytesRead = thingSpeakSocket.Receive(buffer);
					// Append the chunk to the string.
					page = page + new String(Encoding.UTF8.GetChars(buffer));
				}

				Debug.Print(page);
			}			
		}
	}
}

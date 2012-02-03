using System;
using System.Net;
using System.Net.Sockets;
using Controller;

namespace Plugins
{
	public class Thingspeak : OutputPlugin
	{
		~Thingspeak() { Dispose(); }
		public override void Dispose() { }
		private string m_httpPost;

		public Thingspeak() { }

		public Thingspeak(string _key)
		{
			//m_httpPost = String.Concat(m_PostHeader, _key, m_PostFooter);
			m_httpPost = "POST /update HTTP/1.1\n";
			m_httpPost += "Host: api.thingspeak.com\n";
			m_httpPost += "Connection: close\n";
			m_httpPost += "X-THINGSPEAKAPIKEY: " + _key + "\n";
			m_httpPost += "Content-type: application/x-www-form-urlencoded\n";
			m_httpPost += "Content-Length ";			
		}

		public override void EventHandler(object _sender, IPluginData _data)
		{
			// build field string from _data and append to the post string
			string fieldData = "field"+(uint)_data.DataType()+"="+_data.GetValue().ToString("F");

			// add content length to post string, then data
			string postString = m_httpPost + fieldData.Length + "\n\n" + fieldData;
			//m_httpPost = String.Concat(m_httpPost, fieldData.Length, "\n\n", fieldData);

			Microsoft.SPOT.Debug.Print(postString);
			//Open the Socket and post the data
			// create required networking parameters
			/*
			using (Socket thingSpeakSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				thingSpeakSocket.Connect(new IPEndPoint((Dns.GetHostEntry("http://api.thingspeak.com")).AddressList[0], 80));
				Byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(m_httpPost);
				thingSpeakSocket.Send(sendBytes, sendBytes.Length, 0);
			}
			 */
		}
	}
}

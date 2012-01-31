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
		private const string m_apiUrl = "http://api.thingspeak.com";
		private const int m_Port = 80;
		private string m_writeApiKey;
		public string WriteApiKey
		{
			set { m_writeApiKey = value; }
		}
		
		private const string m_PostHeader = @"POST /update HTTP/1.1\n
											Host: api.thingspeak.com\n
											Connection: close\n
											X-THINGSPEAKAPIKEY: ";
		private const string m_PostFooter = @"Content-Type: application/x-www-form-urlencoded\n
											Content-Length: ";

		public override int TimerInterval() { return 0; }
		public override Category Category() { return Controller.Category.Output; }
		public override void TimerCallback(object _state) { }

		public Thingspeak() { }

		public override void EventHandler(object _sender, IPluginData _data)
		{
			// build field string from _data and append to the post string
			string fieldData = String.Concat("field",(uint)_data.DataType(),"=",_data.GetValue());

			// add content length to post string, then data
			string httpPost = String.Concat(m_PostHeader, m_writeApiKey, m_PostFooter);				
			httpPost = String.Concat(httpPost, fieldData.Length, "\n\n", fieldData);

			//Open the Socket and post the data
			// create required networking parameters
			using (Socket thingSpeakSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				thingSpeakSocket.Connect(new IPEndPoint((Dns.GetHostEntry(m_apiUrl)).AddressList[0], m_Port));
				Byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(httpPost);
				thingSpeakSocket.Send(sendBytes, sendBytes.Length, 0);
			}
		}
	}
}

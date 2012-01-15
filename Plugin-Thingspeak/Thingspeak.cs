using System;
using System.Net;
using System.Net.Sockets;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Thingspeak : IPlugin
	{
		~Thingspeak() { Dispose(); }
		public void Dispose()
		{
			if(m_thingSpeakSocket != null) m_thingSpeakSocket.Close();
		}
		private const string m_writeApiKey = "AVZ5A0YSSQMJOOYP";
		private const string m_apiUrl = "http://api.thingspeak.com";
		private const int m_Port = 80;
		private Socket m_thingSpeakSocket;
		private string m_httpPost;
		private const string m_PostHeader = @"POST /update HTTP/1.1\n
											Host: api.thingspeak.com\n
											Connection: close\n
											X-THINGSPEAKAPIKEY: ";
		private const string m_PostFooter = @"Content-Type: application/x-www-form-urlencoded\n
											Content-Length: ";

		public int TimerInterval() { return 0; }
		public Category Category() { return Controller.Category.Output; }
		public void TimerCallback(object _state) { }

		public Thingspeak()
		{
			// build up proper http post string
			m_httpPost = String.Concat(m_PostHeader, m_writeApiKey, m_PostFooter);

			// create required networking parameters
			m_thingSpeakSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint apiEndPoint = new IPEndPoint((Dns.GetHostEntry(m_apiUrl)).AddressList[0], m_Port);
			m_thingSpeakSocket.Bind(apiEndPoint);
		}

		public void EventHandler(object _sender, IPluginData _data)
		{
			// build field string from _data and append to the post string
			string fieldData = String.Concat("field",(uint)_data.DataType(),"=",_data.GetValue());

			// add content length to post string, then data
			m_httpPost = String.Concat(m_httpPost, fieldData.Length, "\n\n", fieldData);

			//Open the Socket and post the data
		}
	}
}

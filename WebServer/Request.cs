using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Collections;
using System.Text;

namespace WebServer
{
	public enum RequestType { Get, Post }

	public class Request : IDisposable
	{		
		const int FileBufferSize = 256;

		public RequestType RequestType { get; protected set; }
		public Socket Client { get; protected set; }
		public string Uri { get; protected set; }
		public string BaseUri { get; protected set; }
		public Hashtable HeaderFields { get; protected set; }
		public Hashtable Querystring { get; protected set; }

		public Request(Socket _client, byte[] _data)
		{
			Client = _client;
			string data = new string(Encoding.UTF8.GetChars(_data));
			string[] requestArray = data.Split('\n');
			if (requestArray.Length > 1)
			{
				// split fist line to get URI information and Request type
				string[] split = requestArray[0].Split(' ');
				if (split.Length >= 2)
				{
					// get request type
					split[0] = split[0].ToUpper();
					switch (split[0])
					{
						case "GET":
							RequestType = WebServer.RequestType.Get;
							break;
						case "POST":
							RequestType = WebServer.RequestType.Post;
							break;
						default:
							throw new NotImplementedException("Can only handle GET and POST");
					}
					// Now that we have the URI, parse out the query string
					Uri = split[1];
					string baseUri = "";
					Querystring = HttpGeneral.ParseQuerystring(Uri, out baseUri);
					BaseUri = baseUri;
				}

				if (split.Length >= 3) HeaderFields.Add("HttpVersion", split[2]);
			}
		}

		public void Dispose() { }
	}
}

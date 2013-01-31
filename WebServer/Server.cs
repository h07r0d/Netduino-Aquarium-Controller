using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Extensions;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using Webserver.POST;
using Webserver.Responses;

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
	/// <param name="results">This Object gets converted into JSON on response</param>
	public delegate void JSONResponseMethod(Request e, Object results);
	
	/// <summary>
	/// Main class of NeonMika.Webserver
	/// </summary>
	public class Server
	{
		public int _PortNumber { get; private set; }
		private Socket _ListeningSocket = null;
		private Hashtable _Responses = new Hashtable();
		private Thread _webserverThread;


		/// <summary>
		/// Creates an NeonMika.Webserver instance
		/// </summary>
		/// <param name="portNumber">The port to listen for incoming requests</param>
		public Server(int portNumber = 80, bool DhcpEnable = true, string ipAddress = "", string subnetMask = "", string gatewayAddress = "")
		{
			var interf = NetworkInterface.GetAllNetworkInterfaces()[0];

			if (DhcpEnable)
			{
				//Dynamic IP
				interf.EnableDhcp();
				//interf.RenewDhcpLease( );
			}
			else
			{
				//Static IP
				interf.EnableStaticIP(ipAddress, subnetMask, gatewayAddress);
			}

			Debug.Print("Webserver is running on " + interf.IPAddress + " /// DHCP: " + interf.IsDhcpEnabled);

			this._PortNumber = portNumber;

			ResponseListInitialize();

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
		private void WaitingForRequest()
		{
			while (true)
			{
				try
				{
					using (Socket clientSocket = _ListeningSocket.Accept())
					{
						Debug.Print("Client Connected");
						int availableBytes = 0;
						int newAvBytes = 0;
						Thread.Sleep(100);

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

						if (availableBytes > 0)
						{
							byte[] buffer = new byte[availableBytes > Settings.MaxRequestSize ? Settings.MaxRequestSize : availableBytes];
							byte[] header = new byte[0];

							int readByteCount = clientSocket.Receive(buffer, buffer.Length, SocketFlags.None);
							Debug.Print(readByteCount + " bytes read");

							for (int headerend = 0; headerend < buffer.Length - 3; headerend++)
							{
								if (buffer[headerend] == '\r' && buffer[headerend + 1] == '\n' && buffer[headerend + 2] == '\r' && buffer[headerend + 3] == '\n')
								{
									header = new byte[headerend + 4];
									Array.Copy(buffer, 0, header, 0, headerend + 4);
									break;
								}
							}

							//reqeust created, checking the response possibilities
							using (Request tempRequest = new Request(Encoding.UTF8.GetChars(header), clientSocket))
							{
								Debug.Print("... Client connected ... URL: " + tempRequest.URL + " ... Final byte count: " + availableBytes);

								if (tempRequest.Method == "POST")
								{
									//POST was incoming, it will be saved to SD card at Settings.POST_TEMP_PATH
									PostToSdWriter post = new PostToSdWriter(tempRequest, buffer, header.Length);
									post.Receive();
								}

								//Let's check if we have to take some action or if it is a file-response 
								HandleGETResponses(tempRequest);
							}

							Debug.Print("Client loop finished");

							try
							{
								//Close client, otherwise the browser / client won't work properly
								clientSocket.Close();
							}
							catch (Exception ex)
							{
								Debug.Print(ex.ToString());
							}
						}
					}
				}
				catch (Exception ex)
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

			if (_Responses.Contains(e.URL))
				response = (Response)_Responses[e.URL];
			else
				response = (Response)_Responses["FileResponse"];


			if (response != null)
			{
				using (response)
				{
					if (response.ConditionsCheckAndDataFill(e))
					{
						if (!response.SendResponse(e))
							Debug.Print("Sending response failed");
					}
				}
			}
			Debug.Print("Request handling finnished");
		}

		/// <summary>
		/// Adds a Response
		/// </summary>
		/// <param name="response">XMLResponse that has to be added</param>
		public void AddResponse(Response response)
		{
			if (!_Responses.Contains(response.URL))
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
			if (_Responses.Contains(ResponseName))
			{
				_Responses.Remove(ResponseName);
			}
		}

		/// <summary>
		/// Initialize the basic functionalities of the webserver
		/// </summary>
		private void ResponseListInitialize()
		{
			AddResponse(new FileResponse());
			AddResponse(new JSONResponse("SaveConfig", new JSONResponseMethod(SaveConfig)));
		}

		/// <summary>
		/// Store config var from browser down onto SD Card and restart the device.
		/// </summary>
		/// <param name="e">Request object from the browser</param>
		/// <param name="ret">Return parameters</param>
		private void SaveConfig(Request e, Object ret)
		{
			try
			{
				// Open LastPost
				FileStream fs = new FileStream(Settings.ConfigFile, FileMode.OpenOrCreate, FileAccess.Write);
				PostFileReader post = new PostFileReader();

				// before new config settings are written down, config must be stored as a JS var, so write in the preamble
				byte[] preamble = Encoding.UTF8.GetBytes("var config=");
				fs.Write(preamble, 0, preamble.Length);

				for (int i = 0; i < post.Length / Settings.FileBufferSize; i++)
					fs.Write(post.Read(Settings.FileBufferSize), 0, Settings.FileBufferSize);
				fs.Write(post.Read((int)(post.Length % Settings.FileBufferSize)), 0, (int)(post.Length % Settings.FileBufferSize));

				//finally, close the js var declaration
				preamble = Encoding.UTF8.GetBytes(";");
				fs.Write(preamble, 0, preamble.Length);
				fs.Flush();
				fs.Close();
				post.Close();
			}
			catch (Exception ex)
			{
				Debug.Print(ex.ToString());
				ret = XMLResponse.GenerateErrorHashtable("file access", ResponseErrorType.InternalOperationError);
			}
		}
	}
}
using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.IO;

namespace Controller
{
    /// <summary>
    /// Holds information about a web request
    /// </summary>
    /// <remarks>
    /// Will expand as required, but stay simple until needed.
    /// </remarks>
    public class Request : IDisposable
    {
        private string method;
        private string url;
        private Socket client;

        const int fileBufferSize = 256;

        internal Request(Socket Client, char[] Data)
        {
            client = Client;
            ProcessRequest(Data);
        }

        /// <summary>
        /// Request method
        /// </summary>
        public string Method
        {
            get { return method; }
        }

        /// <summary>
        /// URL of request
        /// </summary>
        public string URL
        {
            get { return url; }
        }

        /// <summary>
        /// Client IP address
        /// </summary>
        public IPAddress Client
        {
            get
            {
                IPEndPoint ip = client.RemoteEndPoint as IPEndPoint;
                if (ip != null) return ip.Address;
                return null;
            }
        }

        /// <summary>
        /// Send a response back to the client
        /// </summary>
        /// <param name="response"></param>
        public void SendResponse(string response, string type = "text/html")
        {
            if (client != null)
            {
                string header = "HTTP/1.0 200 OK\r\nContent-Type: " + type + "; charset=utf-8\r\nContent-Length: " + response.Length.ToString() + "\r\nConnection: close\r\n\r\n";

                client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
                client.Send(Encoding.UTF8.GetBytes(response), response.Length, SocketFlags.None);

                Debug.Print("Response of " + response.Length.ToString() + " sent.");
            }

        }

        /// <summary>
        /// Sends a file back to the client
        /// </summary>
        /// <remarks>
        /// Assumes the application using this has checked whether it exists
        /// </remarks>
        /// <param name="filePath"></param>
        public void SendFile(string filePath)
        {
            // Map the file extension to a mime type
            string type = "";
            int dot = filePath.LastIndexOf('.');
            if (dot != 0)
                switch (filePath.Substring(dot + 1))
                {
                    case "css":
                        type = "text/css";
                        break;
                    case "xml":
                    case "xsl":
                        type = "text/xml";
                        break;
                    case "jpg":
                    case "jpeg":
                        type = "image/jpeg";
                        break;
                    case "gif":
                        type = "image/gif";
                        break;
					default:
						type = "text/html";
						break;
                    // Not exhaustive. Extend this list as required.
                }


            using (FileStream inputStream = new FileStream(filePath, FileMode.Open))
            {
                // Send the header
                string header = "HTTP/1.0 200 OK\r\nContent-Type: " + type + "; charset=utf-8\r\nContent-Length: " + inputStream.Length.ToString() + "\r\nConnection: close\r\n\r\n";
                client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);

                byte[] readBuffer = new byte[fileBufferSize];
                while (true)
                {
                    // Send the file a few bytes at a time
                    int bytesRead = inputStream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead == 0)
                        break;

                    client.Send(readBuffer, bytesRead, SocketFlags.None);
                    Debug.Print("Sending " + readBuffer.Length.ToString() + "bytes...");
                }
            }

            Debug.Print("Sent file " + filePath);
        }

        /// <summary>
        /// Send a Not Found response
        /// </summary>
        public void Send404()
        {
            string header = "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
            if (client != null)
                client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
            Debug.Print("Sent 404 Not Found");
        }
        /// <summary>
        /// Process the request header
        /// </summary>
        /// <param name="data"></param>
        private void ProcessRequest(char[] data)
        {
            string content = new string(data);
			Debug.Print(content);
            string firstLine = content.Substring(0, content.IndexOf('\n'));

            // Parse the first line of the request: "GET /path/ HTTP/1.1"
            string[] words = firstLine.Split(' ');
            method = words[0];
            url = words[1];

			if (url.Equals("/"))
				url = "/index.html";
            // Could look for any further headers in other lines of the request if required (e.g. User-Agent, Cookie)
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
        }

        #endregion
    }
}

using System;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using Webserver.EventArgs;
using Microsoft.SPOT;

namespace Webserver.Responses
{
    abstract public class Response : IDisposable
    {
        private string _url;

        public string URL
        {
            get { return _url; }
            set { _url = value; }
        }

        /// <summary>
        /// Creates response to send back to client
        /// </summary>
        /// <param name="beforeFileSearchMethods">Webserver expand methods</param>
        public Response(string Name)
        {
            this._url = Name;
        }

        /// <summary>
        /// Creates header for 200 OK response
        /// </summary>
        /// <param name="MimeType">MIME type of response</param>
        /// <param name="ContentLength">Byte count of response body</param>
        protected void Send200_OK(string MimeType, int ContentLength, Socket Client)
        {
            /*
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("HTTP/1.0 200 OK\r\n");
            headerBuilder.Append("Content-Type: ");
            headerBuilder.Append(MimeType);
            headerBuilder.Append("; charset=utf-8\r\n");
            headerBuilder.Append("Content-Length: ");
            headerBuilder.Append(ContentLength.ToString());
            headerBuilder.Append("\r\n");
            headerBuilder.Append("Connection: close\r\n\r\n");
             * */

            String header = "HTTP/1.0 200 OK\r\n" + "Content-Type: " + MimeType + "; charset=utf-8\r\n" + "Content-Length: " + ContentLength.ToString() + "\r\n" + "Connection: close\r\n\r\n";

            try
            {
                Client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message.ToString());
                return;
            }
        }

        /// <summary>
        /// Sends a 404 Not Found response
        /// </summary>
        protected void Send404_NotFound(Socket Client)
        {
            string header = "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\nWebserver is sorry, but:\nPage not found";
            if (Client != null)
                Client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
            Debug.Print("Sent 404 Not Found");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Filename">File name or complete file path</param>
        /// <returns>MIME type</returns>
        protected string MimeType(string Filename)
        {
            string result = "text/html";
            int dot = Filename.LastIndexOf('.');

            string ext = (dot >= 0) ? Filename.Substring(dot + 1) : "";
            switch (ext)
            {
                case "htm":
                case "html":
                    result = "text/html";
                    break;
                case "js":
                    result = "text/javascript";
                    break;
                case "css":
                    result = "text/css";
                    break;
                case "xml":
                case "xsl":
                    result = "text/xml";
                    break;
                case "jpg":
                case "jpeg":
                    result = "image/jpeg";
                    break;
                case "gif":
                    result = "image/gif";
                    break;
                case "png":
                    result = "image/png";
                    break;
                case "ico":
                    result = "x-icon";
                    break;
                case "mid":
                    result = "audio/mid";
                    break;
                default:
                    result = "application/octet-stream";
                    break;
            }
            return result;
        }

        protected bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 & part2)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Override this method to implement a response logic.
        /// </summary>
        /// <returns>True if Response was sent, false if not</returns>
        abstract public bool SendResponse(RequestReceivedEventArgs requestArguments);

        /// <summary>
        /// Override this, check the URL and process data if needed
        /// </summary>
        /// <returns>True if SendResponse should be sent, false if not</returns>
        abstract public bool ConditionsCheckAndDataFill(RequestReceivedEventArgs requestArguments);

        #region IDisposable Members

        public void Dispose()
        {
            
        }

        #endregion
    }
}

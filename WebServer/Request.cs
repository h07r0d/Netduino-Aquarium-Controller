using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Collections;

namespace Webserver
{
    //ToDo: Post-Values have to be copied into hashtable "postContent"

    public class Request : IDisposable
    {
        protected string _method;
        protected string _url;
        protected Hashtable _postArguments = new Hashtable();
        protected Hashtable _getArguments = new Hashtable();

        /// <summary>
        /// Hashtable with all GET key-value pa in it
        /// </summary>
        public Hashtable GetArguments
        {
            get { return _getArguments; }
            private set { _getArguments = value; }
        }

        /// <summary>
        /// HTTP verb (Request method)
        /// </summary>
        public string Method
        {
            get { return _method; }
            private set { _method = value; }
        }

        /// <summary>
        /// URL of request without GET values
        /// </summary>
        public string URL
        {
            get { return _url; }
            private set { _url = value; }
        }

        /// <summary>
        /// ToDo: Post-Values have to be copied into hashtable
        /// Full POST line is saved to "post"-key
        /// </summary>
        public Hashtable PostContent
        {
            get { return _postArguments; }
        }

        /// <summary>
        /// Creates request
        /// </summary>
        /// <param name="Data">Input from network</param>
        public Request(char[] Data)
        {
            ProcessRequest(Data);
        }
       
        /// <summary>
        /// Sets up the request
        /// </summary>
        /// <param name="data">Input from network</param>
        private void ProcessRequest(char[] data)
        {
            string content = new string(data);
            string htmlHeader1stLine = content.Substring(0, content.IndexOf('\n'));

            // Parse the first line of the request: "GET /path/ HTTP/1.1"
            string[] headerParts = htmlHeader1stLine.Split(' ');
            _method = headerParts[0];
            string[] UrlAndParameters = headerParts[1].Split('?');
            _url = UrlAndParameters[0].Substring(1); // Substring to ignore the '/'

            if (_method.ToUpper() == "GET" && UrlAndParameters.Length > 1)
            {
                FillGETHashtable(UrlAndParameters[1]);
            }
   
            if (_method.ToUpper() == "POST") // TODO!
            {
                int lastLine = content.LastIndexOf('\n');
                _postArguments.Clear();
                _postArguments.Add("post", content.Substring(lastLine + 1, content.Length - lastLine));
            }
            else
                _postArguments = null;

            // Could look for any further headers in other lines of the request if required (e.g. User-Agent, Cookie)
        }

        /// <summary>
        /// builds arguments hash table
        /// </summary>
        /// <param name="value"></param>
        private void FillGETHashtable(string url)
        {
            _getArguments = new Hashtable();

            string[] urlArguments = url.Split('&');
            string[] keyValuePair;

            for (int i = 0; i < urlArguments.Length; i++)
            {
                keyValuePair = urlArguments[i].Split('=');
                _getArguments.Add(keyValuePair[0], keyValuePair[1]);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if(_postArguments != null)
                _postArguments.Clear();
            
            if(_getArguments != null)
                _getArguments.Clear();
        }

        #endregion
    }
}
using System;
using Microsoft.SPOT;
using System.Text;
using System.Threading;
using System.Collections;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using System.Net.Sockets;

namespace WebServer
{
    public enum EnumContentType { Text, Binary, MultipartFormData } //add as neccessary
    public enum EnumRequestType { Get, Post, Put } //add as neccessary

    public class BaseRequest
    {
        public EnumRequestType HttpRequestType { get; protected set; }
        public string Uri { get; protected set; }
        public string BaseUri { get; protected set; }
        public string HttpVersion { get; protected set; }
        public string Host { get; protected set; }
        public string UserAgent { get; protected set; }
        public Hashtable HeaderFields { get; protected set; }
        public Hashtable QuerystringVariables { get; protected set; }
        

        protected BaseRequest()
        {
        }

        public BaseRequest(byte[] bRawRequest, int contentStart, int contentSize, Hashtable headerFields, EnumRequestType requestType)
        {
            LoadBaseRequest(ref bRawRequest, contentStart, contentSize, headerFields, requestType);
        }

        protected void LoadBaseRequest(ref byte[] bRawRequest, int contentStart, int contentSize, Hashtable headerFields, EnumRequestType requestType)
        {
            HttpRequestType = requestType;
            HeaderFields = headerFields;
            // Convert to string, will include HTTP headers.
            string content = new string(Encoding.UTF8.GetChars(bRawRequest, contentStart, contentSize));// there shouldn't be content

            // pull out uri
            Uri = headerFields["Uri"].ToString();// url is deprecated?
            string baseUri = "";
            QuerystringVariables = GetQueryStringVariables(Uri, out baseUri);
            BaseUri = baseUri;
            //parse uri for command processing. 
            //start with the 2nd character to avoid a blank entry.
            //            ParsedUri = Uri.Substring(1).Split(new char[] { '/' });
        }

        public static void LoadHeader(byte[] rawRequest, int rawRequestSize, out int headerLength, out Hashtable header, out EnumRequestType requestType)  // returns the start position of the unused bytes
        {
            header = new Hashtable();
            requestType = GetRequestType(ref rawRequest);
            string requestString = new string(Encoding.UTF8.GetChars(rawRequest, 0, rawRequestSize));
            headerLength = GetHeaderLength(ref rawRequest);
            string[] headerArray = requestString.Split('\n');
            if (headerArray.Length > 1)
            {
                string[] split = headerArray[0].Split(' ');
                if (split.Length >= 2)
                {
                    // first line GET /index.html?userid=joe&password=guessme HTTP/1.1
                    header.Add("Uri", split[1]);
                }
                if (split.Length >= 3)
                {
                    header.Add("HttpVersion", split[2]);
                }

            }
            for (int i = 1; i < headerArray.Length; i++)
            {
                AddFieldToHashTable(header, headerArray[i]);
            }
        }

        protected static int GetHeaderLength(ref byte[] buffer)
        {
            int contentStart = -1;
            for (int i = 0; i < buffer.Length - 3; i++)
            {// find a double "carriage return",  "line feed" to give the end of the header, "0xd, 0xa, 0xd, 0xa"
                if (buffer[i] == 0xd)
                {
                    if (buffer[i + 1] == 0xa)
                    {
                        if (buffer[i + 2] == 0xd)
                        {
                            if (buffer[i + 3] == 0xa)
                            {
                                contentStart = i + 4;
                                break;
                            }
                        }
                    }
                }
            }
            return contentStart;
        }

        protected static void AddFieldToHashTable(Hashtable fields, string keyValuePair) // Todo fix, because User-Agent and other fields may have spaces
        {
            string[] split = keyValuePair.Split(new char[] { ':'},2);
            if (split.Length > 1)
            {
                if (split[0] == "Content-Type")
                {
                    split = keyValuePair.Split(new char[] { ':', ';', '=' });// todo refactor
                    int numberOfPairs = split.Length / 2;
                    for (int i = 0; i < numberOfPairs; i++)
                    {
                        //Host: www.mysite.com
                        //User-Agent: Mozilla/4.0    
                        //"Content-Type: multipart/form-data; boundary=---------------------------41184676334\r"
                        string key = split[2 * i].Trim(new char[] { ' ', '\r' });
                        string value = split[2 * i + 1].Trim(new char[] { ' ', '\r' });
                        {
                            if (!fields.Contains(key))// don't add twice it may raise inconvenient exception
                            {
                                fields.Add(key, value);
                                Debug.Print(key + " : " + value);
                            }
                        }
                    }
                }
                else
                {
                    string key = split[0];
                    string value = split[1].Trim(new char[] { '\r' });
                    {
                        if (!fields.Contains(key))// don't add twice it may raise inconvenient exception
                        {
                            fields.Add(key, value);
                            Debug.Print(key + " : " + value);
                        }
                    }
                }
            }
        }

        protected Hashtable GetQueryStringVariables(string uri, out string baseUrl)
        {
            baseUrl = uri;
            Hashtable result = new Hashtable();
            if (uri != null)
            {
                string[] split = uri.Split('?');
                if (split.Length > 1)
                {
                    baseUrl = split[0];
                    string partQueryString = split[1];
                    result = GetEncodedVariables(partQueryString);
                }

            }
            return result;
        }

        protected Hashtable GetEncodedVariables(string variableString)
        {
            Hashtable result = new Hashtable();
            if (variableString != null)
            {
                string[] split = variableString.Split('&');
                if (split.Length > 0)
                {// remove querystring ?
                    split[0] = split[0].TrimStart(new char[] { '?', ' ' });
                }
                for (int i = 0; i < split.Length; i++)
                {
                    string[] parameterArray = split[i].Split('=');

                    string value = "";
                    if (parameterArray.Length > 1)
                    {
                        value = parameterArray[1].Trim();
                        value = HttpGeneral.UriDecode(value);
                    }
                    string key = parameterArray[0].Trim();
                    if (!result.Contains(key))
                    {
                        result.Add(key, value);
                    }
                }
            }
            return result;
        }

        private static EnumRequestType GetRequestType(ref byte[] bRawRequest)
        {
            string rawRequestType = new string(Encoding.UTF8.GetChars(bRawRequest, 0, 20));
            rawRequestType = rawRequestType.TrimStart().ToUpper();
            if (rawRequestType.IndexOf("GET") == 0)
            {
                return EnumRequestType.Get;
            }
            else
            {
                if (rawRequestType.IndexOf("PUT") == 0)
                {
                    return EnumRequestType.Put;
                }
                else
                {
                    return EnumRequestType.Post;
                }
            }
        }
    }

    //public class GetRequest:BaseRequest
    //{
    //    //GET /index.html?userid=joe&password=guessme HTTP/1.1
    //    //Host: www.mysite.com
    //    //User-Agent: Mozilla/4.0

    //    public GetRequest(byte[] buffer, int dataStart, int dataEnd) : base(buffer, dataLength)
    //    {
    //        IsHttpGet = true;
    //        HeaderFields = GetHeader(buffer, dataLength);
    //    }
    //}


    public class PostRequest : BaseRequest
    {
        public int ContentLength { get; private set; }
        public string ContentType { get; private set; }		
        public EnumContentType ContentTypeGroup { get; private set; }
        public int ContentStartPosition { get; private set; }
        public Hashtable ContentVariables { get; protected set; }
        public Hashtable ContentVariablesBinary { get; protected set; }
        public GetMoreBytesHandler GetMoreBytesHandler = null;
        public Socket ConnectionSocket = null;// todo can make private?

        //POST /login.jsp HTTP/1.1
        //Host: www.mysite.com
        //User-Agent: Mozilla/4.0
        //Content-Length: 27
        //Content-Type: application/x-www-form-uriencoded

        //userid=joe&password=guessme



        //or also 
        //Content-Type: application/octet-stream for binary

        public PostRequest(byte[] bRawRequest, int contentStart, int contentSize, Hashtable headerFields, EnumRequestType requestType, Socket connectionSocket, GetMoreBytesHandler getMoreBytesHandler)
            : base(bRawRequest, contentStart, contentSize, headerFields, requestType)
        {
            ContentTypeGroup = EnumContentType.Text;// todo check this maybe remove
            GetMoreBytesHandler = getMoreBytesHandler;
            ConnectionSocket = connectionSocket;
			ContentStartPosition = contentStart;
			ContentLength = contentSize;
            // pull out uri
            Uri = headerFields["Uri"].ToString();
            ContentType = HeaderFields["Content-Type"].ToString();
            if(HeaderFields.Contains("Content-Length"))
            {
                int contentLengthFromHeader = int.Parse(HeaderFields["Content-Length"].ToString());
                if( contentLengthFromHeader != ContentLength)
                {
                    ; // means more packets needed
                }
            }

			if (ContentTypeGroup == EnumContentType.Text)
			{
				string content = new string(Encoding.UTF8.GetChars(bRawRequest, ContentStartPosition, ContentLength - ContentStartPosition));
				//ContentVariables = GetEncodedVariables(content);
			}


            //if (ContentType.ToLower().IndexOf("multipart/form-data") >= 0)
            //{    //    http://www.4guysfromrolla.com/webtech/091201-1.shtml check filesize before uploading
            //    byte[] boundary = null;// todo get this.
            //    int boundaryLength = -1;
            //    int[] boundaryDelimiterPositions = null;
            //    ContentTypeGroup = EnumContentType.MultipartFormData;
            //    GetMultipartFormTextVariables(buffer, ContentStartPosition, boundary, out boundaryDelimiterPositions, out boundaryLength);
            //    ContentVariables = GetMultipartFormTextVariables(buffer, boundaryDelimiterPositions, boundaryLength);
            //    ContentVariablesBinary = GetMultipartFormBinaryVariables(buffer, boundaryDelimiterPositions, boundaryLength);
            //}
            //else
            //{
            //    if (ContentType.ToLower().IndexOf("application/octet-stream") >= 0)
            //    {
            //        ContentTypeGroup = EnumContentType.Binary;
            //        throw new NotImplementedException("application/octet-stream not implemented.");                  
            //    }
            //    else
            //    {
            //        ContentTypeGroup = EnumContentType.Text;
            //        //Content-Type: application/x-www-form-urlencoded Todo are there any other options?
            //        string content = new string(Encoding.UTF8.GetChars(buffer, ContentStartPosition, dataLength - ContentStartPosition));
            //        ContentVariables = GetEncodedVariables(content);
            //    }
            //}


        }

        public void GetMultipartFormTextVariables(byte[] data, int ContentStartPosition, byte[] boundary, out int[] boundaryDelimiterPositions, out int boundaryLength)
        {
            boundaryDelimiterPositions = null;// TODO
            boundaryLength = -1;
        }


        public Hashtable GetMultipartFormTextVariables(byte[] data, int[] boundaryDelimiterPositions, int boundaryLength)
        {
            Hashtable result = new Hashtable();// TODO
//            string content = new string(Encoding.UTF8.GetChars(bRawRequest, ContentStartPosition, bRawRequest.Length - ContentStartPosition));
            return result;
        }

        public Hashtable GetMultipartFormBinaryVariables(byte[] data, int[] boundaryDelimiterPositions, int boundaryLength)
        {
            Hashtable result = new Hashtable();// TODO
//            string content = new string(Encoding.UTF8.GetChars(bRawRequest, ContentStartPosition, bRawRequest.Length - ContentStartPosition));
            return result;
        }
    }
}
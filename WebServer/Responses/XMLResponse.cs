using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Collections;
using Webserver.EventArgs;
using Microsoft.SPOT;

namespace Webserver.Responses
{
    public class XMLResponse : Response
    {
        public XMLResponse(string url, XMLResponseMethod method)
            : base(url)
        {
            this._ResponseMethod = method;
            _Pairs = new Hashtable();
        }

        private XMLResponseMethod _ResponseMethod;
        private Hashtable _Pairs;

        public static Hashtable GenerateErrorHashtable(String parameter, ResponseErrorType ret)
        {
            Hashtable h = new Hashtable();

            switch (ret)
            {
                case ResponseErrorType.ParameterConvertError:
                    h.Add("error","Following parameter could not be converted: " + parameter + " to the right format (int, string, ...).");
                    break;
                case ResponseErrorType.ParameterMissing:
                    h.Add("error","Following parameter was not submitted: " + parameter + ". Please include it in your URL");
                    break;
                case ResponseErrorType.InternalValueNotSet:
                    h.Add("error","An internal error accured. Following value could not be set: " + parameter + ". Please check the requested method's source code");
                    break;
                case ResponseErrorType.ParameterRangeException:
                    h.Add("error","Following parameter was out of range: " + parameter + ".");
                    break;
            }

            return h;
        }

        /// <summary>
        /// Execute this to check if SendResponse shoul be executed
        /// </summary>
        /// <param name="RequestArguments">Event Args</param>
        /// <returns>True if URL refers to this method, otherwise false (false = SendRequest should not be exicuted) </returns>
        public override bool ConditionsCheckAndDataFill(RequestReceivedEventArgs RequestArguments)
        {
            _Pairs.Clear();
            if (RequestArguments.Request.URL == this.URL)
                _ResponseMethod(RequestArguments, _Pairs);
            else
                return false;
            return true;
        }


        public void SetError()
        { }

        /// <summary>
        /// Sends XML to client
        /// </summary>
        /// <param name="requestArguments">Could be null</param>
        /// <returns>True if 200_OK was sent, otherwise false</returns>
        public override bool SendResponse(RequestReceivedEventArgs requestArguments)
        {
            String xml = "";
            xml += "<!--XML created by NeonMika Webserver-->";

            xml += "<Response>";

            //xml += XMLConverter.ConvertHashtableToXMLString(_Pairs);

            xml += "</Response>";

            byte[] bytes = Encoding.UTF8.GetBytes(xml);

            int byteCount = bytes.Length;

            try
            {
                Send200_OK("text/xml", byteCount, requestArguments.Client);
                requestArguments.Client.Send(bytes, byteCount, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }

            return true;
        }

        
    }
}

using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Webserver.EventArgs
{
    public class RequestReceivedEventArgs
    {
        private DateTime receiveTime;
        private Request request;
        private Socket client;
        private int byteCount;

        public DateTime ReceiveTime
        {
            get { return receiveTime; }
        }

        public Request Request
        {
            get { return request; }
        }

        public Socket Client
        {
            get { return client; }
        }

        public int ByteCount
        {
            get { return byteCount; }
        }

        public RequestReceivedEventArgs(Request request, Socket client, int byteCount)
        {
            this.request = request;
            this.client = client;
            this.byteCount = byteCount;

            this.receiveTime = DateTime.Now;
        }
    }
}

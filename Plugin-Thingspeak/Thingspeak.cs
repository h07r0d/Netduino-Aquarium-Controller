using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Controller;
using Microsoft.SPOT;
using System.Threading;

namespace Plugins
{
    public class Thingspeak : OutputPlugin
    {
        ~Thingspeak() { Dispose(); }
        public override void Dispose() { }
        private string m_httpPost;
        private DateTime NextUpdateTime;
        string NextFieldData;
        private const string m_thingSpeakIP = "api.thingspeak.com";

        public Thingspeak() { }

        public Thingspeak(object _config)
        {
            Hashtable config = (Hashtable)_config;
            m_httpPost = "POST /update HTTP/1.1\nHost: api.thingspeak.com\nConnection: close\nX-THINGSPEAKAPIKEY: ";
            m_httpPost += config["writeapi"].ToString() + "\n";
            m_httpPost += "Content-Type: application/x-www-form-urlencoded\nContent-Length: ";
            NextUpdateTime = DateTime.Now;
            NextFieldData = "";
        }

        private static Socket ConnectSocket(String server, Int32 port)
        {
            // Get server's IP address.
            IPHostEntry hostEntry = Dns.GetHostEntry(server);

            // Create socket and connect to the server's IP address and port
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 5000;
            socket.SendTimeout = 5000;
            socket.Connect(new IPEndPoint(hostEntry.AddressList[0], port));
            return socket;
        }

        public override void EventHandler(object _sender, IPluginData _data)
        {
            try
            {
                //Build ThingSpeak Post Data
                string fieldData = "";
                foreach (PluginData _pd in _data.GetData())
                {
                    if (_pd.LastReadSuccess)
                    {
                        // build field string from _data and append to the post string
                        if (NextFieldData.Contains("field" + _pd.ThingSpeakFieldID.ToString()))
                        {
                            string[] _Fields = NextFieldData.Split('&');
                            NextFieldData = "";
                            foreach (string f in _Fields)
                                if (!(f.Contains("field" + _pd.ThingSpeakFieldID.ToString())))
                                    NextFieldData += f + "&";
                        }
                        else
                            fieldData += "field" + _pd.ThingSpeakFieldID + "=" + _pd.Value.ToString("F") + "&";
                    }
                }
                // add the last fielddata to the current and remove the last & from the string.
                fieldData += NextFieldData;
                NextFieldData = "";
                fieldData = fieldData.TrimEnd('&');

                //Only post if there is some data.
                if (!(fieldData == "") && NextUpdateTime <= DateTime.Now)
                {
                    string postString = m_httpPost + fieldData.Length + "\n\n" + fieldData;
                    //Debug.Print("Post String: " + postString);
                    //Open the Socket and post the data
                    // create required networking parameters

                    using (Socket thingSpeakSocket = ConnectSocket(m_thingSpeakIP, 80))
                    {
                        Byte[] sendBytes = Encoding.UTF8.GetBytes(postString);
                        thingSpeakSocket.Send(sendBytes, sendBytes.Length, 0);

                        // wait for a response to see what happened
                        Byte[] buffer = new Byte[256];
                        String page = String.Empty;

                        // Poll for data until 30-second timeout.  Returns true for data and connection closed.
                        while (thingSpeakSocket.Poll(20 * 1000000, SelectMode.SelectRead))
                        {
                            // If there are 0 bytes in the buffer, then the connection is closed, or we have timed out.
                            if (thingSpeakSocket.Available == 0) break;

                            // Zero all bytes in the re-usable buffer.
                            Array.Clear(buffer, 0, buffer.Length);

                            // Read a buffer-sized HTML chunk.
                            Int32 bytesRead = thingSpeakSocket.Receive(buffer);
                            // Append the chunk to the string.
                            page = page + new String(Encoding.UTF8.GetChars(buffer));
                        }
                        thingSpeakSocket.Close();
                        //Update the time that the next update can happen.
                        NextUpdateTime = DateTime.Now.AddSeconds(15);
                        //Debug.Print(fieldData + " " + "transmitted to ThingSpeak");
                        //Debug.Print("Received: " + page.ToString());
                        if (page.Contains("1\r\n0\r\n0"))
                        {
                            //Reschedule for future posting if it failed.
                            NextFieldData = fieldData;
                            throw new Exception("Failed to post data to ThingSpeak.  Rescheduled post.");
                        }
                    }
                }
                else
                {
                    //Store the fieldData for future post
                    NextFieldData = fieldData;
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
            }
        }
    }
}

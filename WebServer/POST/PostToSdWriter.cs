using System;
using Microsoft.SPOT;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using Webserver.Responses;

namespace Webserver.POST
{
    /// <summary>
    /// Saves a POST-request at Setting.POST_TEMP_PATH
    /// Sends back "OK" on success
    /// </summary>
    class PostToSdWriter : IDisposable
    {
        private byte[] _buffer;
        private int _startAt;
        private Request _e;

        public PostToSdWriter(Request e, byte[] buffer, int startAt)
        {
            _buffer=buffer;
            _startAt = startAt;
            _e = e;
        }

        /// <summary>
        /// Saves content to Setting.POST_TEMP_PATH
        /// </summary>
        /// <param name="e">The request which should be handeld</param>
        /// <returns>True if 200_OK was sent, otherwise false</returns>
        public bool Receive()
        {
            Debug.Print(Debug.GC(true).ToString());
            Debug.Print(Debug.GC(true).ToString());

            int availableBytes = Convert.ToInt32(_e.Headers["Content-Length"].ToString().TrimEnd('\r'));
			char[] bufferContents = Encoding.UTF8.GetChars(_buffer);
			Debug.Print(new string(bufferContents));

            try
            {
                FileStream fs = new FileStream(Settings.POST_TEMP_PATH, FileMode.Create, FileAccess.Write);
                Debug.Print(Debug.GC(true).ToString());
                Debug.Print(Debug.GC(true).ToString());
                
                fs.Write(_buffer,_startAt,_buffer.Length-_startAt);
                availableBytes -= (_buffer.Length-_startAt);

                _buffer = new byte[availableBytes > Settings.MAX_REQUESTSIZE ? Settings.MAX_REQUESTSIZE : availableBytes];

                while (availableBytes > 0)
                {
                    if(availableBytes < Settings.MAX_REQUESTSIZE)
                        _buffer = new byte[availableBytes];

                   // while (_e.Client.Available < _buffer.Length)
                   //     Thread.Sleep(1);

                    _e.Client.Receive(_buffer, _buffer.Length, SocketFlags.None);
					bufferContents = Encoding.UTF8.GetChars(_buffer);
					//object json = Extensions.JSON.JsonDecode(new string(bufferContents));
					Debug.Print(new string(bufferContents));
                    fs.Write(_buffer, 0, _buffer.Length);
                    availableBytes -= Settings.MAX_REQUESTSIZE;
                }

                fs.Flush();
                fs.Close();
            }
            catch (Exception ex)
            {
                Debug.Print("Error writing POST-data");
                return false;
            }

            return true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _buffer = new byte[0];            
        }

        #endregion
    }
}

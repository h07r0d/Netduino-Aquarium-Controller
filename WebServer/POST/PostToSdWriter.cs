using System;
using Microsoft.SPOT;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using Webserver.Responses;
using Webserver;

namespace NeonMika.Webserver.POST
{
    /// <summary>
    /// Saves a POST-request at Setting.POST_TEMP_PATH
    /// Sends back "OK" on success
    /// </summary>
    class PostToSdWriter : IDisposable
    {
        private byte[] _buffer;
        private Request _e;

        public PostToSdWriter(Request e, byte[] buffer)
        {
            _buffer=buffer;
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
            try
            {
                if (_e.URL == "SaveConfig")
                {
                    FileStream fs = new FileStream(Settings.LAST_POST, FileMode.Create, FileAccess.Write);
                    fs.Write(_buffer,0,_buffer.Length);
                    fs.Flush();
                    fs.Close();
                }
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

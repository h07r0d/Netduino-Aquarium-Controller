using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using Webserver.EventArgs;
using Microsoft.SPOT;
using Extensions;

namespace Webserver.Responses
{
    /// <summary>
    /// Standard response sending file to client
    /// </summary>
    public class FileResponse : Response
    {
        public FileResponse(String name = "FileResponse")
            : base(name)
        { }

        public override bool  ConditionsCheckAndDataFill(RequestReceivedEventArgs RequestArguments)
        {
            return true;
        }

        public override bool SendResponse(RequestReceivedEventArgs RequestArguments)
        {
            string filePath = Settings.ROOT_PATH + RequestArguments.Request.URL;

			// replace any URL paths with file paths.
			filePath.Replace("/", "\\");
			Debug.Print(filePath);

            //File found check
            try
            {
                if (!File.Exists(filePath))
                {
                    Send404_NotFound(RequestArguments.Client);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }

            string mType = MimeType(filePath);

            //File sending
            using (FileStream inputStream = new FileStream(filePath, FileMode.Open))
            {
                Send200_OK(mType, (int)inputStream.Length, RequestArguments.Client);

                byte[] readBuffer = new byte[Settings.FILE_BUFFERSIZE];
                int sentBytes = 0;

                //Sending parts in size of "Settings.FILE_BUFFERSIZE"
                while (sentBytes < inputStream.Length)
                {
                    int bytesRead = inputStream.Read(readBuffer, 0, readBuffer.Length);
                    try
                    {
                        if (SocketConnected(RequestArguments.Client))
                        {
                            sentBytes += RequestArguments.Client.Send(readBuffer, bytesRead, SocketFlags.None);
                        }
                        else
                        {
                            RequestArguments.Client.Close();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.ToString());
                        try
                        {
                            RequestArguments.Client.Close();
                        }
                        catch (Exception ex2)
                        {
                            Debug.Print(ex2.ToString());
                        }

                        return false;
                    }
                }
            }

            return true;
        }
    }
}

       
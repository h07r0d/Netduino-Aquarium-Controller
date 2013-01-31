using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using Microsoft.SPOT;
using System.Collections;
using Microsoft.SPOT.Net.NetworkInformation;

namespace Webserver.Responses
{
    /// <summary>
    /// Standard response sending file to client
    /// If filename is a directory, a directory-overview will be displayed
    /// </summary>
    public class FileResponse : Response
    {
        public FileResponse(String name = "FileResponse")
            : base(name)
        { }

        /// <summary>
        /// File response has no conditions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override bool  ConditionsCheckAndDataFill(Request e)
        {
            return true;
        }

        /// <summary>
        /// Depending on the requested path, a file, a directory-overview or a 404-error will be returned
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override bool SendResponse(Request e)
        {
			string filePath;
			if (e.URL.Length > 0)
				filePath = Settings.ROOT_PATH + e.URL;
			else
				filePath = Settings.ROOT_PATH + "/index.html";
            bool isDirectory = false;

            char[] chars = filePath.ToCharArray();
            filePath="";

            for (int i = 0; i < chars.Length; i++)
                if (chars[i] == '/')
                    filePath+="\\";
                else
                    filePath+=chars[i];

            chars = new char[0];

            //File found check
            if ( !CheckFileDirectoryExist(ref filePath, out isDirectory) )
                Send404_NotFound(e.Client);

            if (isDirectory)
            {
                ArrayList toReturn = new ArrayList();
                string send;
                var interf = NetworkInterface.GetAllNetworkInterfaces()[0];

                Send200_OK(MimeType(".html"), 0, e.Client);

                string uppath = ((filePath.LastIndexOf("\\") >= 0) ? interf.IPAddress + ((filePath[0] != '\\') ? "\\" : "") + filePath.Substring(0, filePath.LastIndexOf("\\")) : interf.IPAddress + ((filePath[0] != '\\') ? "\\" : "") + filePath);

                send = "<html><head><title>" + e.URL + "</title>" +
                    "<style type=\"text/css\">a.a1{background-color:#ADD8E6;margin:0;padding:0;font-weight:bold;}a.a2{background-color:#87CEEB;margin:0;padding:0;font-weight:bold;}</style>" +
                    "</head><body><a href=\"http:\\\\" + uppath + "\">One level up</a><br/>" +
                    "<h1>" + e.URL + "</h1><h2>Directories:</h2>";
                if (SendData(e.Client, Encoding.UTF8.GetBytes(send)) == 0)
                    return false;

                foreach (string d in Directory.GetDirectories(filePath))
                {
                    send = "<a href=\"http:\\\\" + interf.IPAddress + d + "\" class=\"a1\">" + d + "</a><br/>";
                    if (SendData(e.Client, Encoding.UTF8.GetBytes(send)) == 0)
                        return false;        
                }

                SendData(e.Client, Encoding.UTF8.GetBytes("<h2>Files:</h2>"));

                foreach (string f in Directory.GetFiles(filePath))
                {
                    send = "<a href=\"http:\\\\" + interf.IPAddress + f + "\" class=\"a2\">" + f + "</a><br/>";
                    if (SendData(e.Client, Encoding.UTF8.GetBytes(send)) == 0)
                        return false;
                }
                
                send = "</body></html>";
                if (SendData(e.Client, Encoding.UTF8.GetBytes(send)) == 0)
                    return false;
            }
            else
            {
                string mType = MimeType(filePath);

                //File sending
                using (FileStream inputStream = new FileStream(filePath, FileMode.Open))
                {
                    Send200_OK(mType, (int)inputStream.Length, e.Client);

                    byte[] readBuffer = new byte[Settings.FILE_BUFFERSIZE];
                    int sentBytes = 0;

                    //Sending parts in size of "Settings.FILE_BUFFERSIZE"
                    while (sentBytes < inputStream.Length)
                    {
                        int bytesRead = inputStream.Read(readBuffer, 0, readBuffer.Length);
                        try
                        {
                            if (SocketConnected(e.Client))
                            {
                                sentBytes += e.Client.Send(readBuffer, bytesRead, SocketFlags.None);
                            }
                            else
                            {
                                e.Client.Close();
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print("Error at sending bytes");
                            try
                            {
                                e.Client.Close();
                            }
                            catch (Exception ex2)
                            {
                                Debug.Print("Error at closing socket");
                            }

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool CheckFileDirectoryExist(ref string filePath, out bool isDirectory)
        {
            isDirectory = false;
            //File found check
            try
            {
                if ( filePath == "" )
                    return false;

                if ( !File.Exists(filePath) )
                    if ( !Directory.Exists(filePath) )
                    {
                        isDirectory = false;
                        return false;
                    }
                    else
                        isDirectory = true;
                return true;
            }
            catch ( Exception ex )
            {
                Debug.Print("Error accessing file/directory");
                Debug.Print(ex.ToString( ));
                return false;
            }
        }
    }
}

       
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;

// This code has been modified from Freds Webserver, thanks Fred.
namespace WebServer
{
    public delegate string ResponseHandler(BaseRequest request);
    public delegate void WorkFinishedHandler(Exception ex, WebResponseThread requestWorker);
    public delegate void SocketAcceptedHandler(WebResponseThread requestWorker);
    public delegate byte[] GetMoreBytesHandler(Socket connectionSocket, out int count);

    public class WebResponseThread
    {
        const int _LanPacketByteCount = 1024;
        const int _PostRxBufferSize = 1024;
        private Thread currentThread = null;        
        private ResponseHandler _ResponseHandler = null;
        private WorkFinishedHandler _WorkFinishedHandler = null;
        private SocketAcceptedHandler _SocketAcceptedHandler = null;
        public int WorkerID = -1;
        private Socket _ConnectionSocket = null;

        /// <summary>
        /// Instantiates a new webthread.
        /// </summary>
        /// <param name="port">Port number to listen on.</param>
        public WebResponseThread(ResponseHandler responseHandler, WorkFinishedHandler workFinishedHandler, SocketAcceptedHandler socketAcceptedHandler)
        {           
            _ResponseHandler = responseHandler;
            _WorkFinishedHandler = workFinishedHandler;
            _SocketAcceptedHandler = socketAcceptedHandler;
        }

        /// <summary>
        /// Start the server thread.
        /// </summary>
        public void Start(Socket nonBlockingSocket)
        {        
            // start server
            this.currentThread = new Thread(ThreadMain);
            _ConnectionSocket = nonBlockingSocket.Accept();// connect in WebServer thread not Web response thread
            currentThread.Start();
            Thread.Sleep(1); // context switch hopefully
            Debug.Print("Started server in thread " + currentThread.GetHashCode().ToString());
        }

        /// <summary>
        /// Allows an external process to stop the thread completely.
        /// </summary>
        public void Stop()
        {
            Debug.Print("Stopping this request.");
            //bring down the thread.  
            this.currentThread.Abort();
        }

        /// <summary>
        /// Runs the server in its own Thread.
        /// </summary>
        private void ThreadMain()
        {
            Exception workException = null;
            try
            {
                Debug.Print("Accepted Socket, workerid: " + this.WorkerID.ToString());
                _SocketAcceptedHandler(this);
                if (_ConnectionSocket.Poll(-1, SelectMode.SelectRead))
                {
                    // Create buffer and receive raw bytes.

                    byte[] firstLanPacket = new byte[_LanPacketByteCount];
                    int count = 0;
                    SocketFlags socketFlags = new SocketFlags();
                    count = _ConnectionSocket.Receive(firstLanPacket, count, _ConnectionSocket.Available - count, socketFlags);
                    if (firstLanPacket[0] == 0)
                    {
                        Debug.Print("empty request, count: " + count.ToString());
                        workException = new Exception("Empty Packet found" + this.WorkerID.ToString());// exception not thrown just sent back
                    }
                    else
                    {
                        string rawHeader = new string(Encoding.UTF8.GetChars(firstLanPacket));
                        Hashtable headerFields = null;
                        int contentStart = -1;
                        EnumRequestType requestType = EnumRequestType.Get;// check for get first
                        BaseRequest.LoadHeader(firstLanPacket, count, out contentStart, out headerFields, out requestType);
                        BaseRequest request = null;
                        try
                        {
                            if (requestType == EnumRequestType.Get)
                            {
                                request = new BaseRequest(firstLanPacket, count, 0, headerFields, requestType);// no content
                            }
                            else
                            {
                                if (requestType == EnumRequestType.Post)
                                {
                                    request = new PostRequest(firstLanPacket, count, 0, headerFields, requestType, _ConnectionSocket, GetMoreBytes);// todo put delegate
                                }
                                else
                                {
                                    throw new NotImplementedException("Http PUT is not implemented.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //return server 500;
                            workException = ex;
                            SendErrorResponse(_ConnectionSocket, SupportedErrors.ServerError, request, ex, this.WorkerID);
                            request = null;
                        }
//                            ConsoleWrite.CollectMemoryAndPrint(true, this.WorkerID);
                        //requestQueue.Enqueue(request);todo
                        if (request != null)
                        {
                            string requestMimeType = GetMimeType(request.Uri);
							Debug.Print("Request MIME Type: " + requestMimeType);
                            if (requestMimeType.Length > 0)
                            {   // file request
                                try
                                {									
									string fullFilePath = SDCard.GetFullPathFromUri(request.BaseUri);
                                    Debug.Print("File Request received:" + fullFilePath);
                                    // send start
									long fileSize = SDCard.GetFileSize(fullFilePath);
                                    if (fileSize > 0)
                                    {
                                        int maxAge = GetMaxAgeFromMimeType(requestMimeType);
                                        string header = HttpGeneral.GetHttpHeader(fileSize, requestMimeType, maxAge);
                                        byte[] returnBytes = Encoding.UTF8.GetBytes(header);
                                        _ConnectionSocket.Send(returnBytes, returnBytes.Length, SocketFlags.None);
                                        SDCard.ReadInChunks(fullFilePath, _ConnectionSocket);// boolean result doesnt really do much so ignore.
                                    }
                                    else
                                    {
                                        SendErrorResponse(_ConnectionSocket, SupportedErrors.FileNotFound, request, null, this.WorkerID);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //return server 500;
                                    workException = ex;
                                    SendErrorResponse(_ConnectionSocket, SupportedErrors.ServerError, request, ex, this.WorkerID);
                                }
                            }
                            else
                            {// page request
                                if (_ResponseHandler != null)
                                {
                                    try
                                    {
                                        string response = _ResponseHandler(request);
                                        byte[] returnBytes = Encoding.UTF8.GetBytes(response);
                                        _ConnectionSocket.Send(returnBytes, 0, returnBytes.Length, SocketFlags.None);
                                    }
                                    catch (Exception ex)
                                    {
                                        //return server 500;
                                        workException = ex;
                                        SendErrorResponse(_ConnectionSocket, SupportedErrors.ServerError, request, ex, this.WorkerID);
                                    }
                                }
                            }
                        }
                    }
                }
            }// using statement can throw in the dispose.
            catch (Exception ex)
            {
                workException = ex;
                Debug.Print("Exception, workerid: " + this.WorkerID.ToString() + ", ex: " + ex.Message);
            }
            finally
            {
                if (_ConnectionSocket != null)
                {
                    ((IDisposable)_ConnectionSocket).Dispose();
                }
                _WorkFinishedHandler(workException, this);
            }
        }

        private void SendErrorResponse(Socket connectionSocket, SupportedErrors errorType, BaseRequest request, Exception ex, int workerID)
        {
            string errorTypeText = ""; 
            if (errorType == SupportedErrors.ServerError)
            {
                errorTypeText = "500 Server Error";
                Debug.Print("SendErrorResponse, workerid: " + workerID.ToString() + ", ex: " + ex.Message + " " + errorTypeText);
            }
            else
            {
                errorTypeText = "404 File not found";
                Debug.Print("SendErrorResponse, workerid: " + workerID.ToString() + " " + errorTypeText);
            }
            string header = "";
            string content = HtmlGeneral.HtmlStart + "<h1>" + errorTypeText + "</h1>" + "<h3>Uri: " + request.Uri + "</h3>";
            if (ex != null)
            {
                content += "<p>Error: " + ex.Message + "</p>";
            }
            content += HtmlGeneral.HtmlEnd;
            if (errorType == SupportedErrors.ServerError)
            {
                header = HttpGeneral.Get500Header(content.Length);
            }
            else
            {
                header = HttpGeneral.Get404Header(content.Length);
            }
            byte[] returnBytes = Encoding.UTF8.GetBytes(header + content);
            connectionSocket.Send(returnBytes, 0, returnBytes.Length, SocketFlags.None);
        }

        public byte[] GetMoreBytes(Socket connectionSocket, out int count)
        {
            byte[] result = new byte[_PostRxBufferSize];
            SocketFlags socketFlags = new SocketFlags();
            count = connectionSocket.Receive(result, result.Length, socketFlags);
            return result;
        }

		public bool ReadInChunks(string fullPath, Socket socket)
		{
			Debug.Print("Reading file: " + fullPath);
			bool chunkHasBeenRead = false;
			int totalBytesRead = 0;
		
			if (File.Exists(fullPath))
			{
				using (FileStream inputStream = new FileStream(fullPath, FileMode.Open))
				{
					byte[] readBuffer = new byte[SDCard.READ_CHUNK_SIZE];
					while (true)
					{
						// Send the file a few bytes at a time
						int bytesRead = inputStream.Read(readBuffer, 0, readBuffer.Length);
						if (bytesRead == 0)
							break;
						socket.Send(readBuffer, 0, bytesRead, SocketFlags.None);						
						totalBytesRead += bytesRead;
					}
				}
				chunkHasBeenRead = true;
			}
			else
			{
				chunkHasBeenRead = false;
			}
			
			if (chunkHasBeenRead == true)
				Debug.Print("Sending " + totalBytesRead.ToString() + " bytes...");
			else
				Debug.Print("Failed to read chunk, full path: " + fullPath);
			return chunkHasBeenRead;
		}


        public int GetMaxAgeFromMimeType(string mimeType)
        {
            int result = 7 * 24 * 60 * 60;// 1 week
            if(mimeType == "text/plain" || mimeType == "text/html")
                result = 10;
            return result;
        }


        public string GetMimeType(string uri)
        {
            // Map the file extension to a mime type
            string type = "";
            int questionMarkIndex = uri.IndexOf('?');
            string baseRequest = uri;
            if (questionMarkIndex >= 0)
            {
                baseRequest = uri.Substring(0, questionMarkIndex);
            }
            int dot = baseRequest.LastIndexOf('.');
            if (dot >= 0)
            {
                switch (baseRequest.Substring(dot + 1))
                {
                    case "txt":
                        type = "text/plain";
                        break;
                    case "css":
                        type = "text/css";
                        break;
                    case "xml":
                    case "xsl":
                        type = "text/xml";
                        break;
                    case "jpg":
                    case "jpeg":
                        type = "image/jpeg";
                        break;
                    case "gif":
                        type = "image/gif";
                        break;
                    case "htm":
                    case "html":
                        type = "text/html";
                        break;
                    case "js":
                        type = "application/javascript";
                        break;

                    //todo json
                    // Not exhaustive. Extend this list as required.
                }
            }
            return type;
        }



        /// <summary>
        /// Gets or sets the port the server listens on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// implement IDisposable
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}

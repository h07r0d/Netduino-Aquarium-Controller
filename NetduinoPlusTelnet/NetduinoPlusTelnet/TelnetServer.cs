using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.SPOT.Net.NetworkInformation;
using System.IO.Ports;

namespace NetduinoPlusTelnet
{
    public class TelnetServer
    {
        private int port;
        private int connectionBuffers;
        private int maximumConnections;
        private ConnectionHandler[] connHandlers;
        private int connTimeout;
        private Thread connDispatch = null;
        private Socket telnetSocket = null;

        /* Constructor for the TelnetServer class. */
        public TelnetServer(int timeout = 120, int listenPort = 23, int connBuffer = 1, int maxConn = 3)
        {
            // How many seconds to maintain an active, idle connection?
            connTimeout = timeout;

            // What port? 23 is the Telnet default, but for security might want to set MUCH higher.
            port = listenPort;

            // How many simultanious -NEW- connections will be handled?
            connectionBuffers = connBuffer;

            // How many concurrent -ACTIVE- connections will be handled?
            maximumConnections = maxConn;
        }

        /* This method activates the Telnet Server. The object doesn't listen for connections until this is called. */
        public bool begin(bool sync = false)
        {

            // Usual stuff... Set up the Socket object. Some line like this will probably be in 90% of Netduino+ projects.
            telnetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Ok... No flack about binding to all IP addresses. Trying to follow the KISS principle at least a little...
            telnetSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            // Set up the listener with a buffer backlog value set to connectionBuffers.
            telnetSocket.Listen(connectionBuffers);

            // Load the connHandlers array with references to null ConnectionHanddler objects.
            connHandlers = new ConnectionHandler[maximumConnections];

            // Construct the ConnectionHandler objects.
            for (int i = 0; i < maximumConnections; i++)
            {
                connHandlers[i] = new ConnectionHandler(connTimeout);
            }

            // Start up a thread to handle connection dispatching asynchronously, or don't if told not to.
            if (sync)
            {
                _connectionDispatcher();
            }
            else
            {
                connDispatch = new Thread(_connectionDispatcher);
                connDispatch.Start();
            }

            // Maybe I'll throw in error checking some day, but for now...
            return true;
        }

        /* This method is actually a thread for accepting connections and dispatching them to the connection handler. */
        private void _connectionDispatcher()
        {
            // Dispatch connections forever.
            while (true)
            {
                // Check the status of all handlers.
                foreach (ConnectionHandler c in connHandlers)
                {
                    if (c.isAvailable())
                    {
                        // This handler is available. Listen for a new connection.
                        Socket newConnection = telnetSocket.Accept(); // Blocks thread until a connection is received.

                        // Got a connection! Pass it to the connection handler.
                        c.acceptConnection(newConnection);
                    }
                    else
                    {
                        // No handlers are available - Yield to other threads.
                        Thread.Sleep(0);
                    }
                }
            }
        }
    }

    public class ConnectionHandler
    {
        private bool available;
        private Socket activeConnection = null;
        private Thread ispThread = null;
        private int sessionTimeout;
        private bool connectionOpen;

        // Set up some variables for incoming data here to avoid making unnecessary garbage.
        private byte[] linesToSend;
        private byte[] commandPrompt;
        const int receiveBufSize = 80; // Probably not getting more than a line of text at at time.
        const int commandBufSize = 255;
        private byte[] receiveBuffer = new byte[receiveBufSize];
        private char[] commandBuffer = new char[commandBufSize];
        private int cmdBufWritePos;
        private int readByteCount;

        public ConnectionHandler(int timeout = 300)
        {
            // This is a new object... It is definitely available.
            available = true;

            // Set the connection timeout.
            sessionTimeout = timeout;
        }

        public bool acceptConnection(Socket incomingConnection)
        {
            // If we're already handling a connection, we can't do anything.
            if (!available) return false;

            available = false;

            // Store the new connection.
            activeConnection = incomingConnection;

            // Mark the current connection as open.
            connectionOpen = true;

            // Start a thread for the interactive session processor to take care of the connection.
            ispThread = new Thread(_sessionProcessor);
            ispThread.Start();

            return true;
        }

        // Can this handler instance accept a new connection or is it in use?
        public bool isAvailable()
        {
            return available;
        }

        /* Method to become a thread for handling interactive sessions. */
        private void _sessionProcessor()
        {
            // Set up the start point on the command buffer to the beginning of it.
            cmdBufWritePos = 0;

            // Prepare the Message Of The Day and the command prompt. "\n" means Line Feed, "\r" means Carrage Return.
            linesToSend = Encoding.UTF8.GetBytes("Welcome to the Netduino Plus Telnet Server!\n\r\n\r");
            commandPrompt = Encoding.UTF8.GetBytes("Command> ");

            // Get the initial telnet data from the telnet client.
            readByteCount = activeConnection.Receive(receiveBuffer);

            // Junk it for now.... I'll figure out what to do with this later.
            for (int i = 0; i < readByteCount; i++)
            {
                receiveBuffer[i] = 0;
            }

            // Print the MOTD and the command prompt.
            activeConnection.Send(linesToSend, SocketFlags.None);
            activeConnection.Send(commandPrompt, SocketFlags.None);

            // Loop will be ended via Thread return after the connection is closed.
            while (connectionOpen)
            {
                // Wait for user input, but only until the timeout period is reached.
                activeConnection.Poll((sessionTimeout * 1000000), SelectMode.SelectRead);

                // If the poll returned and nothing is available to read, it either timed out or the connection was closed.
                if (activeConnection.Available == 0) break;

                // We Got some data... Do something with it.
                do
                {
                    // Read the data.
                    readByteCount = activeConnection.Receive(receiveBuffer, receiveBufSize, SocketFlags.None);

                    // *** Data comes across as plain text here. No UTF-8 decode needed.

                    // This method is only broken out for clarity of code.
                    if (!_processReceivedData())
                    {
                        connectionOpen = false; // Drop out of the outer loop as well.
                        break;
                    }

                } while (activeConnection.Available > 0);

            }

            // Socket state is needs to be closed.

            activeConnection.Close();
            activeConnection = null;
            available = true;
            return;

        }

        /* This method appends the received data to the command buffer.             */
        /* The command buffer is necessary in case the command comes in fragmented. */
        private bool _processReceivedData()
        {
            if ((receiveBuffer.Length) > (commandBufSize - cmdBufWritePos))
            {
                // Command buffer will overflow... Clear the buffer and tell the user.
                cmdBufWritePos = 0;
                _sendReplyAndPrompt("Command line is too long!\r\n");
                return true;
            }

            // "Copy" the characters from the receive buffer to the command buffer and check for command completion.
            for (int i = 0; i < readByteCount; i++)
            {
                // Decide if a command is complete or needs attention.
                switch (receiveBuffer[i])
                {
                    case 13: // Line feed. Skip it... Activate on carriage return.
                        break;

                    case 10: // Carriage return.
                        if (!_processCommand(cmdBufWritePos)) return false; // False indicates connection close requested.

                        // Command was handled - Reset the position so further entries land at the beginning.
                        cmdBufWritePos = 0;
                        break;

                    case 9: // Tab key.
                        // Do nothing for Tab right now...
                        break;

                    case 8: // Backspace key.
                        // Do nothing for Backspace right now...
                        break;

                    default: // No action key received. Append the byte to the command buffer as a character.
                        commandBuffer[cmdBufWritePos] = Convert.ToChar(receiveBuffer[i]);
                        // Advance the command buffer write position.
                        cmdBufWritePos++;
                        break;
                }
            }

            return true;
        }

        /* This method processes individual commands from the command buffer.          */
        /* True indicates command processed successfully. False closes the connection. */
        private bool _processCommand(int commandLength)
        {
            // Grab the command and change it to a string for simplicity's sake.
            String commandLine = new String(commandBuffer, 0, commandLength);

            // Split it up into command and arguments.
            String[] command = commandLine.Split(' ');

            // Pick the command and execute it.
            switch (command[0].ToLower())
            {
                // All the hard work is done... This part pretty self-explanatory.
                case "calibrate":
                    if (command.Length > 1) // This command takes 1 argument.
                    {
                        switch (command[1].ToLower()) // What do we do with the LED?
                        {
                            case "ph":
                                _sendReply("Begining PH Calibration. Connected to PH Stamp. Press ^ to Exit.\r\n");
                                _sendReply("1. Type C to enter continues mode.\r\n");
                                _sendReply("2. Place probe in PH 7 for 1-2 mins then enter S.\r\n");
                                _sendReply("3. Place probe in PH 4 for 1-2 mins then enter F.\r\n");
                                _sendReply("4. Place probe in PH 10 for 1-2 mins then enter T.\r\n");
                                _sendReply("5. Press E to end.\r\n");
                                Calibrate(Serial.COM2);
                                _sendReplyAndPrompt("Completed calibration for PH.\r\n");
                                break;
                            case "ec":
                                _sendReply("Begining EC Calibration. Connected to EC Stamp. Press ^ to Exit.\r\n");
                                _sendReply("1. Type C to enter continues mode..\r\n");
                                _sendReply("2. Set Probe Type - P,1 = K0.1 | P,2 = K1.0, P,3 = K10.0.\r\n");
                                _sendReply("3. Dry Calibration - Enter Z0.\r\n");
                                _sendReply("4. Place probe in High(40,000) solution 3-5 minutes and enter Z40.\r\n");
                                _sendReply("5. Place probe in Low(10,500) solution 3-5 minutes and enter Z10.\r\n");
                                Calibrate(Serial.COM1);
                                _sendReplyAndPrompt("Completed calibration for EC.\r\n");
                                break;
                            default:
                                _sendReplyAndPrompt("Please specify 'ph' or 'ec'.\r\n");
                                break;
                        }
                    }
                    else // Not enough arguments!
                    {
                        _sendReplyAndPrompt("Please specify 'ph' or 'ec'.\r\n");
                    }
                    break;
                case "reboot":
                    _sendReply("Device is going down for a reboot.\r\n");
                    Thread.Sleep(2000);
                    PowerState.RebootDevice(false); //Reboot Device
                    break;
                case "exit": // Got to have a way out, right?
                    return false;
                default:
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Available commands are:");
                    sb.AppendLine("calibrate <ec or ph> - Calibrate ph or ec");
                    sb.AppendLine("reboot - Reboot the device.");
                    sb.AppendLine("exit - End the connection.");
                    _sendReplyAndPrompt(sb.ToString());
                    break;
            }
            return true;
        }

        private void _sendReplyAndPrompt(String strToSend)
        {
            Byte[] responseBytes = Encoding.UTF8.GetBytes(strToSend + "Command> ");
            activeConnection.Send(responseBytes, SocketFlags.None);
        }
        private void _sendReply(String strToSend)
        {
            Byte[] responseBytes = Encoding.UTF8.GetBytes(strToSend);
            activeConnection.Send(responseBytes, SocketFlags.None);
        }

        /// <summary>
        /// Relays data between a serial port and the telnet session.
        /// This allows access from telnet to the serial 
        /// port of the EC and PH moduled directly.
        /// </summary>
        /// <param name="SerialPortName">String for the serial port you want to access.</param>
        private void Calibrate(String SerialPortName)
        {
            SerialPort sp = new SerialPort(SerialPortName, 38400, Parity.None, 8, StopBits.One);
            try
            {
                byte ExitCommand = 94; //^ Key
                bool Continue = true;
                int m_LoopCount = 0;
                sp.ReadTimeout = 4000;
                sp.WriteTimeout = 4000;
                sp.Open();
                do
                {
                    m_LoopCount += 1; //Incriment loop count for inactivity.

                    if (activeConnection.Available > 0)
                    {
                        byte[] t_buffer = new byte[activeConnection.Available];
                        activeConnection.Receive(t_buffer);
                        if (t_buffer[0] == ExitCommand)  //exit if ^ is pressed
                        {
                            Continue = false;
                            break;
                        }
                        else
                        {
                            if (t_buffer[t_buffer.Length - 2] == 13 && t_buffer[t_buffer.Length - 1] == 10) //When sending data to the stamp replace crlf with return.
                            {
                                m_LoopCount = 0; //Reset Loop Count
                                sp.Write(t_buffer, 0, t_buffer.Length - 1); //Send message to the serial port
                            }
                            else
                            {
                                m_LoopCount = 0; //Reset Loop Count
                                sp.Write(t_buffer, 0, t_buffer.Length); //Send message to the serial port
                            }
                            sp.Flush();
                        }
                        t_buffer = null;
                    }
                    if (sp.BytesToRead > 0)
                    {
                        byte[] ph_cmd = new byte[sp.BytesToRead];
                        sp.Read(ph_cmd, 0, ph_cmd.Length); //Read from serial port
                        activeConnection.Send(ph_cmd, SocketFlags.None); //Write what was read to telnet session
                        m_LoopCount = 0; //Reset Loop Count
                        ph_cmd = null;
                    }
                    Thread.Sleep(100);
                } while (Continue && m_LoopCount < 60000);  //Time out after 1 minute of inactivity.
            }
            catch (Exception e)
            {
                _sendReply("Error accessing serial port: \r\n" + e.Message + "\r\nIt may be in use.  Try again later.\r\n");
            }
            finally
            {
                //close the serial port if its opened and Dispose it.
                if (sp.IsOpen)
                    sp.Close();
                sp.Dispose();
            }
        }
    }
}

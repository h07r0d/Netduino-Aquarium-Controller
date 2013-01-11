using System;
using System.Collections;
using Controller;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO;
using System.Text;

namespace Plugins
{
    /// <summary>
    /// Control Plugin to manage relays
    /// Relays used can be obtained here: http://www.amazon.com/gp/product/B0057OC5WK/ref=oh_details_o00_s00_i01
    /// </summary>
    public class Relays : ControlPlugin
    {
        ~Relays() { Dispose(); }
        public override void Dispose() { m_relayPins = null; m_commands = null; }
        public override bool ImplimentsEventHandler() { return true; }
        private CommandData[] m_commands;
        private CommandData[] m_ranges;
        private const string m_statusFileName = @"\SD\relaystatus.js";
        public override CommandData[] Commands() { return m_commands; }

        /// <summary>
        /// Iterates through all of the data received and checks it against its stored ranges
        /// and then determines weather or not it needs to turn something on or off based on the range values.
        /// </summary>
        /// <param name="_sender">Object that raised the callback</param>
        /// <param name="_data">Last reading</param>
        public override void EventHandler(object _sender, IPluginData _data)
        {
            foreach (PluginData _pd in _data.GetData())
            {
                foreach (CommandData CMD in m_ranges)
                {
                    if ((_pd.Name == CMD.RangeMetric) && _pd.LastReadSuccess && CMD.Enable)
                        CheckRange(m_relayPins[CMD.RelayID].Read(),_pd,CMD);
                    Debug.GC(true);
                }
            }
        }

        /// <summary>
        /// Used during Command parsing to trigger immediate command execution.
        /// Example: during mid-day, the lights should be on, but a reboot of the controller will restart
        /// the Netduino, forcing a re-parse of the config file.  
        /// The timers will be setup to turn on the next day, but they need to be on immediately as well
        /// </summary>
        private bool m_missedExecute = false;
        private bool ON = false;
        private bool OFF = true;
        private OutputPort[] m_relayPins;
        public Relays(object _config)
        {
            // Initialize Relay Pin controls
            m_relayPins = new OutputPort[12];
            m_relayPins[0] = new OutputPort(Pins.GPIO_PIN_D4, OFF);
            m_relayPins[1] = new OutputPort(Pins.GPIO_PIN_D5, OFF);
            m_relayPins[2] = new OutputPort(Pins.GPIO_PIN_D6, OFF);
            m_relayPins[3] = new OutputPort(Pins.GPIO_PIN_D7, OFF);
            m_relayPins[4] = new OutputPort(Pins.GPIO_PIN_D8, OFF);
            m_relayPins[5] = new OutputPort(Pins.GPIO_PIN_D9, OFF);
            m_relayPins[6] = new OutputPort(Pins.GPIO_PIN_D10, OFF);
            m_relayPins[7] = new OutputPort(Pins.GPIO_PIN_D11, OFF);
            //Relays for ph and nutrient pumps.
            m_relayPins[8] = new OutputPort(Pins.GPIO_PIN_A2, OFF);
            m_relayPins[9] = new OutputPort(Pins.GPIO_PIN_A3, OFF);
            m_relayPins[10] = new OutputPort(Pins.GPIO_PIN_A4, OFF);
            m_relayPins[11] = new OutputPort(Pins.GPIO_PIN_A5, OFF);
            // parse config Hashtable and store array list of commands
            // the individual relays are stored in an Array List
            Hashtable config = (Hashtable)_config;
            ArrayList relays = config["relays"] as ArrayList;
            ParseCommands(relays);
        }

        /// <summary>
        /// Execute RelayCommand given in state object
        /// </summary>
        /// <param name="state">DictionaryEntry object to process on execution</param>
        public override void ExecuteControl(object state)
        {
            // Callback received a RelayCommand struct to execute			
            CommandData command = (CommandData)state;
            if (command.Enable)
            {
                Debug.Print("Setting " + command.RelayName + " to " + (!command.Command).ToString());
                m_relayPins[command.RelayID].Write(command.Command);
                UpdateStatusFile();
            }
        }

        /// <summary>
        /// Parse JSON array of relay config and store for delayed execution
        /// </summary>
        /// <param name="_commands">JSON formatted list of objects</param>
        private void ParseCommands(ArrayList _commands)
        {
            ArrayList CMDs = new ArrayList();
            ArrayList Ranges = new ArrayList();
            foreach (Hashtable command in _commands)
            {
                try
                {
                    CommandData CMD = new CommandData();
                    CommandData CMD_1 = new CommandData();
                    // parse out details from config into the CommandData class
                    CMD.Enable = (command["Enable"].ToString() == "true");
                    CMD.TimerType = command["type"].ToString();
                    CMD.RelayName = command["name"].ToString();
                    CMD.RelayID = int.Parse(command["id"].ToString());

                    CMD_1.Enable = CMD.Enable;
                    CMD_1.TimerType = CMD.TimerType;
                    CMD_1.RelayName = CMD.RelayName;
                    CMD_1.RelayID = CMD.RelayID;

                    switch (CMD.TimerType)
                    {
                        case "DailyTimer":
                            CMD.FirstRun = GetTimeSpan(command["on"].ToString());
                            CMD.Command = ON;
                            CMD.RepeatTimeSpan = new TimeSpan(24, 0, 0);
                            CMDs.Add(CMD);
                            if (m_missedExecute)
                                ExecuteControl(CMD);

                            CMD_1.RepeatTimeSpan = new TimeSpan(24, 0, 0);
                            CMD_1.FirstRun = GetTimeSpan(command["off"].ToString());
                            CMD_1.Command = OFF;
                            CMDs.Add(CMD_1);
                            if (m_missedExecute)
                                ExecuteControl(CMD_1);
                            break;

                        case "Timer":
                            CMD.DurationOff = GetTimeSpanFromArrayList(command["DurationOff"] as ArrayList);
                            CMD.DurationOn = GetTimeSpanFromArrayList(command["DurationOn"] as ArrayList);
                            //Create On Timer
                            CMD.FirstRun = CMD.DurationOff;
                            CMD.RepeatTimeSpan = CMD.DurationOff + CMD.DurationOn;
                            CMD.Command = ON;
                            CMDs.Add(CMD);
                            //Create Off timer
                            CMD_1.DurationOff = CMD.DurationOff;
                            CMD_1.DurationOn = CMD.DurationOn;
                            CMD_1.RepeatTimeSpan = CMD.DurationOff + CMD.DurationOn;
                            CMD_1.FirstRun = CMD_1.DurationOff + CMD_1.DurationOn;
                            CMD_1.Command = OFF;
                            CMDs.Add(CMD_1);
                            break;

                        case "Range":
                            CMD.RangeMetric = command["RangeMetric"].ToString();
                            CMD.RangeMin = double.Parse(command["min"].ToString());
                            CMD.RangeMax = double.Parse(command["max"].ToString());
                            CMD.Inverted = (command["Inverted"].ToString() == "true");
                            CMD.PulseTime = int.Parse(command["PulseTime"].ToString());
                            if (CMD.PulseTime > 0)
                            {
                                CMD.TimeBetweenPulses = int.Parse(command["PulseSpace"].ToString());
                                CMD.NextPulseAfter = DateTime.Now;
                            }
                            Ranges.Add(CMD);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                }
                finally
                {
                    m_commands = new CommandData[CMDs.Count];
                    m_ranges = new CommandData[Ranges.Count];
                    for (int i = 0; i < CMDs.Count; i++)
                    {
                        m_commands[i] = (CommandData)CMDs[i];
                    }
                    for (int i = 0; i < Ranges.Count; i++)
                    {
                        m_ranges[i] = (CommandData)Ranges[i];
                    }
                    Debug.GC(true);
                }
            }
        }

        /// <summary>
        /// Determine the timespan from 'now' when the given command should be run
        /// </summary>
        /// <param name="_time">An ISO8601 formatted time value representing the time of day for execution</param>
        /// <returns>Timespan when task should be run</returns>		
        private TimeSpan GetTimeSpan(string _time)
        {
            // Determine when this event should be fired as a TimeSpan
            // Assuming ISO8601 time format "hh:mm"
            int hours = int.Parse(_time.Substring(0, 2));
            int minutes = int.Parse(_time.Substring(3, 2));
            DateTime now = DateTime.Now;
            DateTime timeToRun = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

            // we missed the window, so setup for next run
            // Also, mark that we missed the run, so we can execute the command during the parse.

            if (timeToRun < now)
            {
                timeToRun = timeToRun.AddDays(1);
                m_missedExecute = true;
            }
            else
                m_missedExecute = false;

            return new TimeSpan((timeToRun - now).Ticks);
        }

        private TimeSpan GetTimeSpanFromArrayList(ArrayList _time)
        {
            // The Timer Interval is specified in an Arraylist of numbers
            //ArrayList interval = config["interval"] as ArrayList;

            //TODO: Double casting since for some reason an explicit cast from a Double
            // to an Int doesn't work.  It's a boxing issue, as interval[i] returns an object.
            // And JSON.cs returns numbers as doubles
            int[] times = new int[3];
            for (int i = 0; i < 3; i++)
            {
                times[i] = (int)(double)_time[i];
            }
            return new TimeSpan(times[0], times[1], times[2]);
        }

        /// <summary>
        /// Checks received values for a sensor against ranges that are configured.
        /// and decides what to do with the relay thats asigned.
        /// </summary>
        /// <param name="PinState">Current power state of the relay</param>
        /// <param name="Sensor">Values from the plugin\sensor</param>
        /// <param name="CMD">Rang settings object</param>
        private void CheckRange(bool PinState, PluginData Sensor, CommandData CMD)
        {
            switch (CMD.Inverted)
            {
                case true:
                    //Inverted
                    if(Sensor.Value <= CMD.RangeMin)
                    {
                        // Reading is below Min
                        // If the appliance is powered off turn it on
                        Debug.Print(Sensor.Name + " is Below Minimum.");
                        if (PinState = OFF)
                        {
                            if (CMD.PulseTime > 0)
                            {
                                if (CMD.NextPulseAfter <= DateTime.Now)
                                {
                                    m_relayPins[CMD.RelayID].Write(ON);
                                    Thread.Sleep(CMD.PulseTime);
                                    m_relayPins[CMD.RelayID].Write(OFF);
                                    Debug.Print("Powering On " + CMD.RelayName + " For " + CMD.PulseTime + " MS");
                                    CMD.NextPulseAfter = DateTime.Now + new TimeSpan(0, CMD.TimeBetweenPulses, 0);
                                }
                                else
                                    Debug.Print("Skipping Range.  Next available time to pulse is: " + CMD.PulseTime.ToString());
                            }
                            else
                            {
                                m_relayPins[CMD.RelayID].Write(ON);
                                Debug.Print("Powering On " + CMD.RelayName);
                            }
                        }
                    }
                    else
                    {
                        // If the Reading is not below the min, power
                        // the appliance off if needed.
                        if(PinState == ON && Sensor.Value >= CMD.RangeMax && CMD.PulseTime == 0)
                        {
                            Debug.Print(Sensor.Name + " is Above Max. Powering Off " + CMD.RelayName);
                            m_relayPins[CMD.RelayID].Write(OFF);
                        }
                    }
                    break;
                case false:
                    //Not Inverted
                    if (Sensor.Value >= CMD.RangeMax)
                    {
                        // Reading is Above Max
                        // If the appliance is powered off turn it on
                        Debug.Print(Sensor.Name + " is Above Maximum.");
                        if (PinState = OFF)
                        {
                            if (CMD.PulseTime > 0)
                            {
                                if (CMD.NextPulseAfter <= DateTime.Now)
                                {
                                    m_relayPins[CMD.RelayID].Write(ON);
                                    Thread.Sleep(CMD.PulseTime);
                                    m_relayPins[CMD.RelayID].Write(OFF);
                                    Debug.Print("Powering On " + CMD.RelayName + " For " + CMD.PulseTime + " MS");
                                    CMD.NextPulseAfter = DateTime.Now + new TimeSpan(0, CMD.TimeBetweenPulses, 0);
                                }
                                else
                                    Debug.Print("Skipping Range.  Next available time to pulse is: " + CMD.PulseTime.ToString());
                            }
                            else
                            {
                                m_relayPins[CMD.RelayID].Write(ON);
                                Debug.Print("Powering On " + CMD.RelayName);
                            }
                        }
                    }
                    else
                    {
                        // If the Reading is not below the min, power
                        // the appliance off if needed.
                        if (PinState == ON && Sensor.Value <= CMD.RangeMin && CMD.PulseTime == 0)
                        {
                            Debug.Print(Sensor.Name + " is Below Min. Powering Off " + CMD.RelayName);
                            m_relayPins[CMD.RelayID].Write(OFF);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Updates relaystatus.js on the sd card with the status of all of the relays.
        /// </summary>
        private void UpdateStatusFile()
        {
            string statusString = "var rs={";
            try
            {
                string State = "";
                foreach(CommandData CMD in m_commands)
                {
                    if (m_relayPins[CMD.RelayID].Read() == ON) State = "ON"; else State = "OFF";
                    if (!statusString.Contains(CMD.RelayName))
                        statusString += '"' + CMD.RelayName  +  '"' + ':' + '"' + State +  '"' + ',';
                }
                foreach (CommandData CMD in m_ranges)
                {
                    if (m_relayPins[CMD.RelayID].Read() == ON) State = "ON"; else State = "OFF";
                    if (!statusString.Contains(CMD.RelayName))
                        statusString += '"' + CMD.RelayName + '"' + ':' + '"' + State + '"' + ',';
                }
                statusString += '"' + "Time" + '"' + ':' + '"' + DateTime.Now.ToString("s") + '"' + '}'+ ';';
                // write relaystatus.js back down to fs, including the var declaration
                byte[] statusBytes = Encoding.UTF8.GetBytes(statusString);
                using (FileStream fs = new FileStream(m_statusFileName, FileMode.Create))
                {
                    fs.Write(statusBytes, 0, statusBytes.Length);
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
            }

        }
    }
}
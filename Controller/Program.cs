using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;


namespace Controller
{
	/// <summary>
	/// Delegate for Output Plugins
	/// </summary>
	/// <param name="_sender">Who sent up the data</param>
	/// <param name="_data">Data sent up from Input Plugin</param>
	public delegate void OutputPluginEventHandler(Object _sender, IPluginData _data);

	/// <summary>
	/// Delegate for Input Plugins
	/// </summary>
	/// <param name="_data">Data sent up from input plugin</param>
	public delegate void InputDataAvailable(IPluginData _data);
	
    public class Program
    {
		

		/// <summary>
		/// Control class for holding Output Plugin weak delegates
		/// </summary>
		private static OutputPluginControl m_opc = new OutputPluginControl();
		public OutputPluginControl OPC { get { return m_opc; } }

		/// <summary>
		/// Delegate for signaling output plugins that data is available
		/// </summary>
		/// <param name="_data">data passed up from input plugin</param>
		private static void DataAvailable(IPluginData _data)
		{
			// data should be available in the queue
			// raise the event to handle it.
			m_opc.ProcessInputData(_data);			
		}

		/// <summary>
		/// Handle to attach to Input Plugin timers.  This event will be raised when an Input Plugin
		/// is processed to trigger Output Plugins to run
		/// </summary>
		private static InputDataAvailable m_inputAvailable = new InputDataAvailable(DataAvailable);

		/// <summary>
		/// Reference to any running timers to ensure they are not GC'd before they run
		/// </summary>
		private static ArrayList m_timers = new ArrayList();
		public ArrayList Timers { get { return m_timers; } }
		
        public static void Main()
        {
			// Initialize required components
			//bootstrap();
			Bootstrap.Start();
			 
			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
			while (true)
			{
				// Blink LED to show we're still responsive
				led.Write(!led.Read());
				Thread.Sleep(500);
			}
        }

		private static void bootstrap()
		{
			// Set system time
			//DS1307 clock = new DS1307();
			//clock.TwelveHourMode = false;
			//Utility.SetLocalTime(clock.CurrentDateTime);
			//clock.Dispose();

					

			/*
			// Load Config file and spin out timers
			Config settings = new Config();
			settings.Load(@"\SD\app.ini");
			
			// loop through config sections and load modules as needed
			foreach (Section item in settings.Sections)
			{
				// skip the plugin if it's disabled
				if (item.Keys["enabled"] == "false")
					continue;

				// Relays use a bunch of timers to handle events
				if (item.Name == "Relays")
				{
					ControlPlugin relayPlugin = LoadControlPlugin(item.Name);					
					foreach (DictionaryEntry relay in item.Keys)
					{
						Debug.Print("Time=" + relay.Key.ToString() + ", Command=" + relay.Value.ToString());
						if (relay.Key.ToString().Substring(0, 4) != "time")
							continue;
						
						DictionaryEntry timerCommand = relayPlugin.ParseCommand(relay.Key.ToString(), relay.Value.ToString());
						Debug.Print("execute in: "+timerCommand.Key.ToString());
						// setup timer to run when configured, and every 24 hours after that.
						// The ExecuteControl will receive an ArrayList of RelayCommands to run when the timer hits
						m_timers.Add(new Timer(relayPlugin.ExecuteControl, timerCommand.Value, (TimeSpan)timerCommand.Key, new TimeSpan(24, 0, 0)));
					}
					continue;
				}
				switch (item.Keys["type"])
				{
					case "input":
						InputPlugin newInputPlugin = LoadInputPlugin(item.Name);
						// spin out a Timer to handle the data, and provide the delegate to pass data back
						TimeSpan timespan = new TimeSpan(0, newInputPlugin.TimerInterval(), 0);
						m_timers.Add(new Timer(newInputPlugin.TimerCallback, m_inputAvailable, timespan, timespan));
						break;
					case "output":
						OutputPlugin newOutputPlugin = null;
						// Special case for Thingspeak plugin, requires an API key from the config file
						if (item.Keys["write_api"] != null)
							newOutputPlugin = LoadOutputPlugin(item.Name, item.Keys["write_api"]);
						else if(item.Keys["location"] != null)
							// Special case for Logfile plugin, requires file name for writing
							newOutputPlugin = LoadOutputPlugin(item.Name, item.Keys["location"]);
						else
							newOutputPlugin = LoadOutputPlugin(item.Name);

						// Add output EventHandler to weak delegate list
						m_opc.DataEvent += newOutputPlugin.EventHandler;
						break;
				}
			}		*/	
		}

		
		
    }
		
	public sealed class OutputPluginControl
	{
		// Holds all the output delegates
		private OutputPluginEventHandler m_eventHandler;

		public void ProcessInputData(IPluginData _data)
		{
			OutputPluginEventHandler ope = m_eventHandler;

			// walk through all available output plugins
			if (ope != null) ope(this, _data);
		}

		public event OutputPluginEventHandler DataEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				m_eventHandler = (OutputPluginEventHandler)WeakDelegate.Combine(m_eventHandler, value);
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				m_eventHandler = (OutputPluginEventHandler)WeakDelegate.Remove(m_eventHandler, value);
			}
		}
	}
}

﻿using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using Extensions;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using Webserver;


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

	/// <summary>
	/// Delegate to handle Web Responses
	/// </summary>
	/// <param name="_sender">Any necessary data to complete the response</param>
	public delegate void WebResponseEventHandler(Object _sender);

	public class Controller
	{
		

		/// <summary>
		/// Utility object to build any static html that can be built on boot
		/// Saves computation time where possible
		/// </summary>
		private static HtmlBuilder m_htmlBuilder;

		/// <summary>
		/// Web Front End server.
		/// <remarks>Needs to be created in order to register plugins to the Web Handlers.</remarks>
		/// </summary>
		private static Webserver.Server m_webServer = new Server();

		/// <summary>
		/// Scheduler for Plugin tasks
		/// </summary>
		private static PluginScheduler m_pluginScheduler;

		/// <summary>
		/// Collection of registered event handlers, mostly for dealing with web requests
		/// </summary>
		private static EventHandlerList m_eventHandlerList = new EventHandlerList();

		/// <summary>
		/// Handle to attach to Input Plugin timers.  This event will be raised when an Input Plugin
		/// is processed to trigger Output Plugins to run
		/// </summary>
		private static InputDataAvailable m_inputAvailable = new InputDataAvailable(DataAvailable);


		/// <summary>
		/// Delegate for signaling output plugins that data is available.
		/// This is called from inside each Input Plugin Callback
		/// </summary>
		/// <param name="_data">data passed up from input plugin</param>
		private static void DataAvailable(IPluginData _data)
		{
			// data should be available in the queue
			// raise the event to handle it.
			OutputPluginEventHandler ope = (OutputPluginEventHandler)m_eventHandlerList["OutputPlugins"];

			// walk through all available output plugins
			if (ope != null) ope(ope, _data);
		}

		
		public static void Main()
		{
			// Initialize required components
			bootstrap();
			// All plugins have been spun out and are running

			//Startup web server front end
			m_webServer.Start();

			Debug.EnableGCMessages(true);
			Debug.Print(Debug.GC(true) + " bytes");

			// Blink LED to show we're still responsive
			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
			while (true)
			{
				led.Write(!led.Read());
				Thread.Sleep(500);
			}
		}

		private static void bootstrap()
		{
			/*
			 * Unfortunately, the DS1307 seems to be causing trouble with any AnalogInputs
			 * when being read from in I2C.  Not sure why....using an NST server instead
			 */

			// Set system time
			DateTime.Now.SetFromNetwork(new TimeSpan(-5, 0, 0));
			//DS1307 clock = new DS1307();
			//clock.TwelveHourMode = false;
			//Utility.SetLocalTime(clock.CurrentDateTime);
			//clock.Dispose();
			Debug.Print(DateTime.Now.ToString());

			m_htmlBuilder = new HtmlBuilder(/* Use local file resources?*/false);
			m_eventHandlerList = new EventHandlerList();
			m_pluginScheduler = new PluginScheduler();

			// Each key in 'config' is a collection of plugin types (input, output, control),
			// so pull out of the root element.
			Hashtable config = ((Hashtable)JSON.JsonDecodeFromVar(Settings.ConfigFile))["config"] as Hashtable;

			// parse each plugin type
			foreach (string pluginType in config.Keys)
				ParseConfig(config[pluginType] as Hashtable, pluginType);

			config = null;
			// config parsed, write out html index
			m_htmlBuilder.GenerateIndex();
			m_htmlBuilder.Dispose();
			Debug.GC(true);

			m_pluginScheduler.Start();
		}

		
		/// <summary>
		/// JSON Object contains nested components which need to be parsed down to indvidual plugin instructions.
		/// This is done recursively to load all necessary plugins
		/// </summary>
		/// <param name="_section">Current Hashtable being processed</param>
		/// <param name="_type">Plugin type being processed</param>
		/// <param name="_name">Name of Plugin being searched for</param>
		private static void ParseConfig(Hashtable _section, string _type = null, string _name = null)
		{			
			foreach (string name in _section.Keys)
			{
				if (_section[name] is Hashtable)
					ParseConfig((Hashtable)_section[name], _type, name);
				else
				{
					// reached bottom of config tree, pass the Hashtable to constructors
					if (_section["enabled"].ToString() == "true")
						LoadPlugin(_name, _type, _section);

					// Include all plugins in web front end, regardless of status, to allow web front end
					// to enable/disable properly
					switch (_type)
					{
						case "input":
							m_htmlBuilder.AddPlugin(_name, PluginType.Input);
							break;
						case "output":
							m_htmlBuilder.AddPlugin(_name, PluginType.Output);
							break;
						case "control":
							m_htmlBuilder.AddPlugin(_name, PluginType.Control);
							break;
						default:
							break;
					}
					return;
				}
			}
		}

		/// <summary>
		/// Load the assembly dynamically from the SD Card, attach any necessary handlers, and update the web front end
		/// to provide config options 
		/// </summary>
		/// <param name="_name">Name of the Plugin</param>
		/// <param name="_type">Class of plugin</param>
		/// <param name="_config"></param>
		private static void LoadPlugin(string _name, string _type, Hashtable _config)
		{
			try
			{
				using (FileStream fs = new FileStream(Settings.PluginFolder + _name + ".pe", FileMode.Open, FileAccess.Read))
				{
					// Create an assembly
					byte[] pluginBytes = new byte[(int)fs.Length];
					fs.Read(pluginBytes, 0, (int)fs.Length);
					Assembly asm = Assembly.Load(pluginBytes);

					foreach (Type type in asm.GetTypes())
					{
						Debug.Print(type.FullName);
						if (type.FullName.Contains(_name))
						{
							// call the constructor with the hashtable as constructor
							// This allows individual plugins to parse out the components they need,
							// and the main application doesn't need to know how each needs to be called
							object plugin = (object)type.GetConstructor(new[] { typeof(object) }).Invoke(new object[] { _config });
							switch (_type)
							{
								case "input":
									// Input plugins should spin out a timer
									InputPlugin ip = (InputPlugin)plugin;
									m_pluginScheduler.AddTask(
										new PluginEventHandler(ip.TimerCallback),
										m_inputAvailable,
										ip.TimerInterval,
										ip.TimerInterval,
										true);

									// There is a special case with the pH plugin.  The pH Stamp can receive a temperature
									// reading to make the pH more accurate. In order to properly update the value, the pH
									// plugin registers an output event to catch a temperature update.
									if (_name.Equals("pH"))
										m_eventHandlerList.AddHandler("OutputPlugins", (OutputPluginEventHandler)ip.EventHandler);

									break;
								case "output":
									// Output plugins need to register an event handler
									OutputPlugin op = (OutputPlugin)plugin;
									m_eventHandlerList.AddHandler("OutputPlugins", (OutputPluginEventHandler)op.EventHandler);
									break;
								case "control":
									// Control Plugins contain a command set that is parsed out into individual timers
									// They also register a Web Response Handler to allow the web front end to call ExecuteControl
									ControlPlugin cp = (ControlPlugin)plugin;
									foreach (DictionaryEntry item in cp.Commands())
										m_pluginScheduler.AddTask(new PluginEventHandler(cp.ExecuteControl),
											item.Value,
											(TimeSpan)item.Key,
											new TimeSpan(24, 0, 0),		// assuming controls should repeat every 24 hours
											true);

									m_webServer.AddResponse(new Webserver.Responses.JSONResponse(_name, cp.HandleWebRequest));
									//m_eventHandlerList.AddHandler(_name, new WebResponseEventHandler(cp.ExecuteControl));
									break;
								default:
									break;
							}
						}
					}
				}
			}
			catch (IOException) { throw; }
			return;
		}
	}
}
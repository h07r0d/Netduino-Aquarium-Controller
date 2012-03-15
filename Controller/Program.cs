using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;


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
		public const string PluginFolder = @"\SD\plugins\";
		public const string FragmentFolder = @"\SD\fragments\";
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

		private static HtmlBuilder m_htmlBuilder;

        public static void Main()
        {
			// Initialize required components
			bootstrap();
			// All plugins have been spun out and are running

			// Startup Web Frontend
			Listener webServer = new Listener(RequestReceived);

			// Blink LED to show we're still responsive
			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
			while (true)
			{				
				led.Write(!led.Read());
				Thread.Sleep(500);
			}
        }

		private static void RequestReceived(Request _request)
		{
			StringBuilder filePath = new StringBuilder();
			filePath.Append("\\SD");
			filePath.Append(_request.URL);
			filePath.Replace("/", "\\");
			Debug.Print(filePath.ToString());
			if (File.Exists(filePath.ToString()))
				_request.SendFile(filePath.ToString());
			else
				_request.Send404();
		}

		private static void bootstrap()
		{
			// Set system time
			DateTime.Now.SetFromNetwork(new TimeSpan(-5, 0, 0));
			//DS1307 clock = new DS1307();
			//clock.TwelveHourMode = false;
			//Utility.SetLocalTime(clock.CurrentDateTime);
			//clock.Dispose();

			m_htmlBuilder = new HtmlBuilder();
			// Each key in 'config' is a collection of plugin types (input, output, control),
			// so pull out of the root element
			Hashtable config = ((Hashtable)JSON.JsonDecodeFromFile(@"\SD\config.js"))["config"] as Hashtable;

			// parse each plugin type
			foreach (string name in config.Keys)
				ParseConfig(config[name] as Hashtable, name);

			// config parsed, write out html index
			m_htmlBuilder.GenerateIndex();
			m_htmlBuilder.Dispose();
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
						
					return;
				}
			}
		}

		private static void LoadPlugin(string _name, string _type, Hashtable _config)
		{
			try
			{
				using (FileStream fs = new FileStream(PluginFolder + _name + ".pe", FileMode.Open, FileAccess.Read))
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
							object plugin = (object)type.GetConstructor(new[] { typeof(object) }).Invoke(new object[] { _config });
							switch (_type)
							{
								case "input":
									// Input plugins should spin out a timer
									InputPlugin ip = (InputPlugin)plugin;
									TimeSpan timespan = new TimeSpan(0, ip.TimerInterval, 0);
									m_timers.Add(new Timer(ip.TimerCallback, m_inputAvailable, timespan, timespan));
									m_htmlBuilder.AddPlugin(_name, PluginType.Input, false);
									break;
								case "output":
									// Output plugins need to register an event handler
									OutputPlugin op = (OutputPlugin)plugin;
									m_opc.DataEvent += op.EventHandler;
									m_htmlBuilder.AddPlugin(_name, PluginType.Output, false);
									break;
								case "control":
									// Control Plugins contain a command set that is parsed out into individual timers
									ControlPlugin cp = (ControlPlugin)plugin;
									foreach (DictionaryEntry item in cp.Commands())									
										m_timers.Add(new Timer(cp.ExecuteControl, item.Value, (TimeSpan)item.Key, new TimeSpan(24, 0, 0)));

									m_htmlBuilder.AddPlugin(_name, PluginType.Control, false);
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

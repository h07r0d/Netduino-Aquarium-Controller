using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Runtime.CompilerServices;

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
		/// Location of Plugins folder
		/// </summary>
		private static readonly string m_pluginFolder = @"\SD\Plugins\";

		/// <summary>
		/// Control class for holding Output Plugin weak delegates
		/// </summary>
		private static OutputPluginControl m_opc = new OutputPluginControl();

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

        public static void Main()
        {
			// Initialize required components
			bootstrap();
			 

			
			

			
					case Category.Output:
						
		

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
			// On startup, set internal clock from DS1307
			DS1307 m_clock = new DS1307();
			// Make sure clock is running
			m_clock.Halt(false);
			Utility.SetLocalTime(m_clock.Get());
			m_clock.Dispose();

			// Load app config and pull enabled plugins
			FileStream config = new FileStream(@"\SD\app.config", FileMode.Open);
			using (XmlReader rdr = XmlReader.Create(config))
			{
				while (rdr.Read())
				{
					// If it's a plugin and it's enabled, process it
					if ( (rdr.NodeType == XmlNodeType.Element) && (rdr.GetAttribute("enabled") == "true") )
					{
						switch (rdr.LocalName)
						{
							case "input":
								InputPlugin newInputPlugin = LoadInputPlugin(rdr.GetAttribute("name"));
								// spin out a Timer to handle the data, and provide the delegate to pass data back
								TimeSpan timespan = new TimeSpan(0, 0/*newPlugin.TimerInterval()*/, 10);
								Timer input = new Timer(newInputPlugin.TimerCallback, m_inputAvailable, timespan, timespan);
								break;
							case "output":
								OutputPlugin newOutputPlugin = LoadOutputPlugin(rdr.GetAttribute("name"));
								// Add output EventHandler to weak delegate list
								m_opc.DataEvent += newOutputPlugin.EventHandler;
								break;
							default:
								break;
						}						
					}
				}
			}
		}

		private static Assembly LoadAssembly(string _name)
		{
			try
			{
				using (FileStream fs = new FileStream(@"\SD\Plugins" + _name + ".pe", FileMode.Open, FileAccess.Read))
				{					
					// Create an assembly
					byte[] pluginBytes = new byte[(int)fs.Length];
					fs.Read(pluginBytes, 0, (int)fs.Length);
					Assembly asm = Assembly.Load(pluginBytes);
					return asm;
				}
			}
			catch (IOException ioe) { throw; }
		}

		private static InputPlugin LoadInputPlugin(string _name, string[] _options = null)
		{
			Assembly asm = LoadAssembly(_name);
				
			//Input Plugins have a TimerCallback function, search for that in the assembly
			foreach (Type type in asm.GetTypes())
			{
				if (type.GetMethod("TimerCallback") != null)
				{
					// It's an Input Plugin, create it and pass back
					return (InputPlugin)type.GetConstructor(new Type[0]).Invoke(new object[0]);
				}
			}
			// couldn't find an appropriate plugin
			return null;
		}

		private static OutputPlugin LoadOutputPlugin(string _name, string[] _options = null)
		{
			Assembly asm = LoadAssembly(_name);

			//Output Plugins have an EventHandler function, search for that in the assembly
			foreach (Type type in asm.GetTypes())
			{
				if (type.GetMethod("EventHandler") != null)
				{
					// It's an output Plugin, create it and pass back
					return (OutputPlugin)type.GetConstructor(new Type[0]).Invoke(new object[0]);
				}
			}
			// couldn't find an appropriate plugin
			return null;
		}

		private static Plugin[] LoadPlugins()
		{
			// parse app.config
			// Determine the number of plugins available
			string[] pluginNames = Directory.GetFiles(m_pluginFolder);
			byte[] pluginBytes;
			FileStream fs;
			FileInfo fi;
			Assembly asm;
			MethodInfo mi;

			// Plugins found, process them and instanciate
			int plugCount = pluginNames.Length;
			Plugin[] plugins = new Plugin[plugCount];
			for (int i = 0; i < plugCount; i++)
			{
				fi = new FileInfo(pluginNames[i]);
				// open the file only if it's an assembly
				if (fi.Extension == ".pe")
				{
					//Open the file and dump to byte array
					try
					{
						using (fs = new FileStream(pluginNames[i], FileMode.Open, FileAccess.Read))
						{
							// Create an assembly
							pluginBytes = new byte[(int)fs.Length];
							fs.Read(pluginBytes, 0, (int)fs.Length);
							asm = Assembly.Load(pluginBytes);

							// figure out properties
							// we only need actual Input and Output plugins
							// if the type does not have a PluginCategory, don't
							// hold it
							foreach (Type test in asm.GetTypes())
							{
								
							}
						}
					}
					catch (IOException) { throw; }				
				}
			}
			return plugins;
		}
    }

	internal sealed class OutputPluginControl
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

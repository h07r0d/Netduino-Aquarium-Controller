using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.IO;
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

        public static void Main()
        {
			// storage for Plugins			
			IPlugin[] m_plugins;
			InputDataAvailable m_inputAvailable = new InputDataAvailable(DataAvailable);

			// parse and add all plugins found in the given folder
			m_plugins = LoadPlugins();
			int pluginCount = m_plugins.Length;

			// process the received plugins
			Category m_pluginCategory = new Category();
			for (int i = 0; i < pluginCount; i++)
			{
				m_pluginCategory = m_plugins[i].Category();
				switch (m_pluginCategory)
				{
					case Category.Input:
						// spin out a Timer to handle the data, and provide the delegate to pass data back
						TimeSpan timespan = new TimeSpan(0, m_plugins[i].TimerInterval(),0);
						Timer input = new Timer(m_plugins[i].TimerCallback, m_inputAvailable, timespan, timespan);						
						break;
					case Category.Output:
						// Add output EventHandler to weak delegate list
						m_opc.DataEvent += m_plugins[i].EventHandler;
						break;
					default:
						break;
				}
			}

			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
			while (true)
			{
				// Blink LED to show we're still responsive
				led.Write(!led.Read());
				Thread.Sleep(500);
			}
        }

		private static IPlugin[] LoadPlugins()
		{
			// Determine the number of plugins available
			string[] pluginNames = Directory.GetFiles(m_pluginFolder);
			byte[] pluginBytes;			
			FileStream fs;
			FileInfo fi;
			Assembly asm;
			MethodInfo mi;
			if (pluginNames.Length > 0)
			{
				// Plugins found, process them and instanciate
				int plugCount = pluginNames.Length;
				IPlugin[] plugins = new IPlugin[plugCount];
				for (int i = 0; i < plugCount; i++)
				{
					fi = new FileInfo(pluginNames[i]);
					// open the file only if it's an assembly
					if (fi.Extension == ".pe")
					{						
						//Open the file and dump to byte array
						using (fs = new FileStream(pluginNames[i], FileMode.Open, FileAccess.Read))
						{
							// Create an assembly
							pluginBytes = new byte[fs.Length];
							fs.Read(pluginBytes, 0, (int)fs.Length);
							asm = Assembly.Load(pluginBytes);
							
							// figure out properties
							// we only need actual Input and Output plugins
							// if the type does not have a PluginCategory, don't
							// hold it
							foreach (Type test in asm.GetTypes())
							{
								if((mi = test.GetMethod("Category"))!= null)
								{
									// Create an object and add to array
									plugins[i] = (IPlugin)test.GetConstructor(new Type[0]).Invoke(new object[0]);
								}								
							}																					
						}
					}
				}
				return plugins;
			}
			// No files found
			return null;			
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

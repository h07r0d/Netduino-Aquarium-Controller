using System;
using Microsoft.SPOT;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Controller
{
	public static class Bootstrap
	{
		/// <summary>
		/// Location of Plugins folder
		/// </summary>
		private static readonly string m_pluginFolder = @"\SD\Plugins\";

		public static void Start()
		{
			// Each key in 'config' is a collection of plugin types (input, output, control),
			// so pull out of the root element
			Hashtable config = ((Hashtable)JSON.JsonDecodeFromFile(@"\SD\config.js"))["config"] as Hashtable;

			// parse each plugin type
			foreach (string name in config.Keys)
			{
				Debug.Print(name);
				ParseConfig(config[name] as Hashtable, name);
				Debug.Print("-- -- --");
			}
		}

		private static void ParseConfig(Hashtable _section, string _type = null)
		{
			
			foreach (string name in _section.Keys)
			{
				if (_section[name] is Hashtable)
				{
					Debug.Print(name+":");
					ParseConfig((Hashtable)_section[name], _type);
				}
				else
				{
					// reached bottom of config tree, all key/value pairs relevant
					foreach (string key in _section.Keys)
						Debug.Print("\t"+key + "=" + _section[key]);

					return;

				}

			}
		}

		private static Assembly LoadAssembly(string _name, string _type=null, Hashtable _config=null)
		{
			try
			{
				using (FileStream fs = new FileStream(m_pluginFolder + _name + ".pe", FileMode.Open, FileAccess.Read))
				{					
					// Create an assembly
					byte[] pluginBytes = new byte[(int)fs.Length];
					fs.Read(pluginBytes, 0, (int)fs.Length);
					Assembly asm = Assembly.Load(pluginBytes);
					return asm;
					/*
					foreach (Type type in asm.GetTypes())
					{
						Debug.Print(type.FullName);
					}
					// load out plugin
					switch (_type)
					{
						case "input":
							
						default:
							break;
					}*/
				}				
			}
			catch (IOException) { throw; }
		}
		
		private static InputPlugin LoadInputPlugin(string _name)
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

		private static ControlPlugin LoadControlPlugin(string _name, string _options = null)
		{
			Assembly asm = LoadAssembly(_name);

			//Control plugins have an ExecuteControl method, check for it
			foreach (Type type in asm.GetTypes())
			{
				if (type.GetMethod("ExecuteControl") != null)
				{
					// Pass options if given
					if(_options != null)
						return (ControlPlugin)type.GetConstructor(new [] { typeof(string) }).Invoke(new object[] { _options });

					return (ControlPlugin)type.GetConstructor(new Type[0]).Invoke(new object[0]);
				}
			}

			// didn't find the correct method
			return null;
		}

		private static OutputPlugin LoadOutputPlugin(string _name, string _options = null)
		{
			Assembly asm = LoadAssembly(_name);

			//Output Plugins have an EventHandler function, search for that in the assembly
			foreach (Type type in asm.GetTypes())
			{
				if (type.GetMethod("EventHandler") != null)
				{
					// It's an output Plugin, create it and pass back
					if (_options != null)
					{
						// Provided options means the Constructor has arguments
						return (OutputPlugin)type.GetConstructor(new[] { typeof(string) }).Invoke(new object[] { _options }); 
					}
					return (OutputPlugin)type.GetConstructor(new Type[0]).Invoke(new object[0]);
				}
			}
			// couldn't find an appropriate plugin
			return null;
		}
	}
}

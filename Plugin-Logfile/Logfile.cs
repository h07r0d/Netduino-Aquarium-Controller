using System;
using System.Collections;
using System.IO;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Logfile : OutputPlugin
	{
		~Logfile() { Dispose(); }
		public override void Dispose() { }		

		private string m_logFile;		

		public Logfile(object _config)
		{			
			Hashtable config = (Hashtable)_config;			
			m_logFile = config["filename"].ToString();			
		}

		public override void EventHandler(Object sender, IPluginData data)
		{
			Debug.Print(data.GetValue().ToString());

			// Format string for output
			string output = DateTime.Now.ToString() +",";
			output += "," + data.DataType().ToString() + ",";
			output += data.GetValue().ToString("F") + ",";
			output += data.DataUnits();

			// take data and write it out to text
			using (StreamWriter sw = new StreamWriter(m_logFile,true))
			{
				sw.WriteLine(output);
				sw.Flush();
			}
		}
	}
}

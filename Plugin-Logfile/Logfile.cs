using System;
using System.Collections;
using System.IO;
using Controller;
using Microsoft.SPOT;
using System.Text;

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
			config = null;
		}

		public override void EventHandler(Object sender, IPluginData data)
		{			
			StringBuilder sb = new StringBuilder();

			// Format string for output
			sb.Append(DateTime.Now.ToString("s"));
			sb.Append(",");
			sb.Append(data.GetValue().ToString("F"));
			sb.Append(data.DataUnits());
			sb.Append("\n");

			// take data and write it out to text
			using (FileStream fs = new FileStream(m_logFile, FileMode.Append))
			{
				var buf = Encoding.UTF8.GetBytes(sb.ToString());
				fs.Write(buf, 0, buf.Length);
				fs.Flush();
			}

			Debug.Print("Written " + sb.ToString());
		}
	}
}

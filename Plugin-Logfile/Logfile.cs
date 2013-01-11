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

        /// <summary>
        /// Appends received data to the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        public override void EventHandler(Object sender, IPluginData data)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (PluginData _PluginData in data.GetData())
                {
                    sb.Append('"');
                    sb.Append(DateTime.Now.ToString("s"));
                    sb.Append('"');
                    sb.Append("," + '"');
                    sb.Append(_PluginData.Name);
                    sb.Append('"' + "," + '"');
                    sb.Append(_PluginData.Value);
                    sb.Append('"' + "," + '"');
                    sb.Append(_PluginData.LastReadSuccess.ToString());
                    sb.AppendLine("\"");
                }
                // take data and write it out to text
                using (FileStream fs = new FileStream(m_logFile, FileMode.Append))
                {
                    var buf = Encoding.UTF8.GetBytes(sb.ToString());
                    fs.Write(buf, 0, buf.Length);
                    fs.Flush();
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
            }
        }
	}
}

using System;
using System.IO;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Logfile : IPlugin
	{
		~Logfile() { Dispose(); }
		public void Dispose() { }
		private readonly char m_comma = ',';
		private readonly string m_logFile = @"\SD\log.txt";
		public Category Category() { return Controller.Category.Output; }
		public int TimerInterval() { return 0; }
		public void TimerCallback(Object state){}
		public void EventHandler(Object sender, IPluginData data)
		{
			Debug.Print("Logfile eventhandler hit\n");
			Debug.Print(data.GetValue().ToString());

			// Format string for output
			string output = String.Concat(DateTime.Now.ToString(), m_comma, data.DataType(), m_comma);
			output = String.Concat(output, m_comma, data.GetValue(), m_comma);
			output = String.Concat(output, m_comma, data.DataUnits());

			// take data and write it out to text
			using (StreamWriter sw = new StreamWriter(m_logFile,true))
			{
				sw.WriteLine(output);
				sw.Flush();
			}
		}
	}
}

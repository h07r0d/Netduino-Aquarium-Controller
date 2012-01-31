using System;
using System.IO;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Logfile : Plugin
	{
		~Logfile() { Dispose(); }
		public override void Dispose() { }
		private readonly char m_comma = ',';
		private readonly string m_logFile = @"\SD\log.txt";
		public override Category Category() { return Controller.Category.Output; }
		public override int TimerInterval() { return 0; }
		public override void TimerCallback(Object state){}
		public override void EventHandler(Object sender, IPluginData data)
		{
			Debug.Print(data.GetValue().ToString());

			// Format string for output
			string output = String.Concat(DateTime.Now.ToString(), m_comma, data.DataType().ToString(), m_comma);
			output = String.Concat(output, data.GetValue().ToString("F"), m_comma);
			output = String.Concat(output, data.DataUnits());

			// take data and write it out to text
			using (StreamWriter sw = new StreamWriter(m_logFile,true))
			{
				sw.WriteLine(output);
				sw.Flush();
			}
		}
	}
}

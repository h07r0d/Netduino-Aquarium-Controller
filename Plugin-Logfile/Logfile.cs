using System;
using System.IO;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class Logfile : IPlugin
	{
		private readonly string _logFile = @"\SD\log.txt";
		public Category Category() { return Controller.Category.Output; }
		public int TimerInterval() { return 0; }
		public void TimerCallback(Object state){}
		public void EventHandler(Object sender, IPluginData data)
		{
			Debug.Print("Logfile eventhandler hit\n");
			Debug.Print(data.GetValue().ToString());
			// take data and write it out to text
			using (StreamWriter sw = new StreamWriter(_logFile,true))
			{
				sw.WriteLine(DateTime.Now.ToString() + "," + data.DataType() + "," + data.GetValue() + "," + data.DataUnits());
				sw.Flush();
			}
		}
	}
}

using System;
using Microsoft.SPOT;
using System.Threading;

namespace Controller
{
	public enum Category : uint { Input=1, Output=2 };
	public interface IPlugin
	{ 
		Category Category();
		int TimerInterval();
		void TimerCallback(Object state);
		void EventHandler(Object sender, IPluginData data);
	}
}

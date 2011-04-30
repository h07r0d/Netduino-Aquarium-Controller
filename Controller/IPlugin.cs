using System;
using Microsoft.SPOT;
using System.Threading;

namespace Controller
{
	public enum Category : uint { Input=1, Output=2 };
    public interface IPlugin
    {
		IPluginData GetData();
		Category PluginCategory();
		
		int PluginTimerInterval();

		/// <summary>
		/// Event for handling Output Plugin data
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		void PluginEventHandler(Object sender, IPluginData data);

		/// <summary>
		/// Event for handling Input Plugin callbacks
		/// </summary>
		/// <param name="state"></param>
		void PluginTimerCallback(Object state);
    }
}

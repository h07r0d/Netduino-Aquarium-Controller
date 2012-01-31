using System;
using Microsoft.SPOT;
using System.Threading;
using System.Reflection;

namespace Controller
{
	public enum Category : uint { Input=1, Output=2 };
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 };
	public interface IPlugin : IDisposable
	{
		bool Enabled();
		Category Category();
		int TimerInterval();
		void TimerCallback(Object state);
		void EventHandler(Object sender, IPluginData data);
	}

	// 
	public abstract class Plugin : IPlugin
	{
		public abstract void Dispose();
		public abstract Category Category();
		public abstract int TimerInterval();
		public abstract void TimerCallback(Object state);
		public abstract void EventHandler(Object sender, IPluginData data);

		/// <summary>
		/// Return enabled status of the plugin.  Assume TRUE if no .config file found.
		/// If a config file is found, check the status of the plugin for enabled
		/// </summary>
		/// <returns>boolean value stating this plugin is active</returns>
		public bool Enabled()
		{
			// determine assembly name
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach(var asm in assemblies)
			{
				Debug.Print(asm.FullName);
			}
			
			return true;
		}
	}

}

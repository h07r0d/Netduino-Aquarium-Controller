using System;

namespace Controller
{
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 };

	public abstract class Plugin : IDisposable
	{
		public abstract void Dispose();		
	}

	public abstract class InputPlugin : Plugin
	{		
		public abstract void TimerCallback(Object state);
		public abstract int TimerInterval();
	}

	public abstract class OutputPlugin : Plugin 
	{
		public abstract void EventHandler(Object sender, IPluginData data);		
	}

	public interface IPluginData
	{
		ThingSpeakFields DataType();
		string DataUnits();
		float GetValue();
	}
}
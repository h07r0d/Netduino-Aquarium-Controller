using System;
using System.Collections;

namespace Controller
{
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 }
	public enum PluginType { Input, Output, Control }

	public abstract class Plugin : IDisposable
	{
		// Implementation for Disposable
		public abstract void Dispose();		
	}

	public abstract class InputPlugin : Plugin
	{
		// Timer Intervals specified in config file
		public abstract TimeSpan TimerInterval { get; }
		public abstract void TimerCallback(Object state);
		public abstract void EventHandler(Object sender, IPluginData data);
	}

	public abstract class OutputPlugin : Plugin 
	{
		public abstract void EventHandler(Object sender, IPluginData data);		
	}

	public abstract class ControlPlugin : Plugin
	{	
		public abstract void ExecuteControl(Object state);
		public abstract Hashtable Commands();
	}

	public interface IPluginData
	{
		ThingSpeakFields DataType();
		string DataUnits();
		float GetValue();
	}
}
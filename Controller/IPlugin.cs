using System;
using Microsoft.SPOT;
using System.Threading;
using System.Reflection;

namespace Controller
{
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 };
	public interface IPlugin : IDisposable
	{
		int TimerInterval();	
	}

	public abstract class Plugin : IPlugin
	{
		public abstract void Dispose();
		public abstract int TimerInterval();
	}

	public abstract class InputPlugin : Plugin
	{		
		public abstract void TimerCallback(Object state);		
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
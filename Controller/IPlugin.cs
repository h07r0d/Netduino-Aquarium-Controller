using System;
using System.Collections;

namespace Controller
{
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 };

	public struct RelayCommand
	{
		public int relay;
		public bool status;

		public RelayCommand(int _relay, bool _status)
		{
			this.relay = _relay;
			this.status = _status;
		}
	}

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

	public abstract class ControlPlugin : Plugin
	{
		public abstract DictionaryEntry ParseCommand(String time, String command);
		public abstract void ExecuteControl(Object state);
	}

	public interface IPluginData
	{
		ThingSpeakFields DataType();
		string DataUnits();
		float GetValue();
	}
}
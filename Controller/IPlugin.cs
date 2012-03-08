using System;
using System.Collections;

namespace Controller
{
	public enum ThingSpeakFields : uint { Temperature = 1, pH = 2 };

	/// <summary>
	/// Simple struct to hold Relay instructions.
	/// The commands are tied to a timespan for execution
	/// </summary>
	public struct RelayCommand
	{
		/// <summary>
		/// ID of the Relay entry to modify
		/// </summary>
		public short relay;

		/// <summary>
		/// Value to write to the Digital I/O pin
		/// </summary>
		public bool status;

		public RelayCommand(short _relay, bool _status)
		{
			this.relay = _relay;
			this.status = _status;
		}
	}

	public abstract class Plugin : IDisposable
	{
		// Implementation for Disposable
		public abstract void Dispose();

		// The controller is configured with a web front end served from the Netduino
		// Each plugin handles it's own html generation, so a filename is provided to
		// the controller pointing to the required html fragment
		public abstract string WebFragment { get; }
	}

	public abstract class InputPlugin : Plugin
	{
		public abstract int TimerInterval { get; }
		public abstract void TimerCallback(Object state);		
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
using System;
using System.Collections;

namespace Controller
{
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
		public abstract void EventHandler(Object sender, IPluginData plugin);
	}

	public abstract class OutputPlugin : Plugin 
	{
		public abstract void EventHandler(Object sender, IPluginData plugin);		
	}

	public abstract class ControlPlugin : Plugin
	{	
		public abstract void ExecuteControl(Object state);
		public abstract Hashtable Commands();
	}

    public interface IPluginData
    {
        PluginData[] GetData();
        void SetData(PluginData[] _value);
    }

    public class PluginData
    {
        
        private string _Name = "";
        private int _ThingSpeakFieldID;
        private string _UnitOfMeasurment="";
        private object _ValOBJ;

        public string Name
        { 
            get { return _Name; }
            set { string Val = _Name; }
        }

        public string UnitOFMeasurment
        {
            get { return _UnitOfMeasurment; }
            set { string Val = _UnitOfMeasurment; } 
        }

        public int ThingSpeakFieldID
        {
            get { return _ThingSpeakFieldID; }
            set { int Val = _ThingSpeakFieldID; }
        }

        public object Value
        {
            get { return _ValOBJ; }
            set { object Val = _ValOBJ; }
        }
    }

}
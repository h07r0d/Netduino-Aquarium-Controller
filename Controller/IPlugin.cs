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
        public abstract bool ImplimentsEventHandler();
	}

	public abstract class OutputPlugin : Plugin 
	{
		public abstract void EventHandler(Object sender, IPluginData plugin);		
	}

	public abstract class ControlPlugin : Plugin
	{	
		public abstract void ExecuteControl(Object state);
        public abstract CommandData[] Commands();
        public abstract void EventHandler(Object sender, IPluginData plugin);
        public abstract bool ImplimentsEventHandler();
	}

    public interface IPluginData
    {
        PluginData[] GetData();
        void SetData(PluginData[] data);
    }

    public class PluginData
    {
        public string Name;
        public uint ThingSpeakFieldID;
        public string UnitOFMeasurment;
        public double Value = 0;
        public bool LastReadSuccess = false;

    }

    public class CommandData
    {
        public TimeSpan FirstRun;
        public bool Command;
        public TimeSpan RepeatTimeSpan;
        public TimeSpan DurationOn;
        public TimeSpan DurationOff;
        public int RelayID;
        public string RelayName;
        public string TimerType;
        public double RangeMin;
        public double RangeMax;
        public bool Inverted;
        public string RangeMetric;
        public bool Enable;
        public int PulseTime;
        public int TimeBetweenPulses;
        public DateTime NextPulseAfter;
    }

}
using Controller;
using CW.NETMF;
using CW.NETMF.Sensors;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Collections;
using System;


namespace Plugins
{	
	public class DHTData : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
	}

	/// <summary>
	/// Plugin for DHT11 & DHT22 Temperature and Humidity sensor.
	/// </summary>
	public class DHTPlugin : InputPlugin
	{
		private TimeSpan m_timerInterval;

		public DHTPlugin() { }		
		public DHTPlugin(object _config) : base()
		{
			Hashtable config = (Hashtable)_config;
			// The Timer Interval is specified in an Arraylist of numbers
			ArrayList interval = config["interval"] as ArrayList;			
		
			//TODO: Double casting since for some reason an explicit cast from a Double
			// to an Int doesn't work.  It's a boxing issue, as interval[i] returns an object.
			// And JSON.cs returns numbers as doubles
			int[] times = new int[3];
			for (int i = 0; i < 3; i++) { times[i] = (int)(double)interval[i]; }
			m_timerInterval = new TimeSpan(times[0], times[1], times[2]);			
		}

        ~DHTPlugin() { Dispose(); }
		public override void Dispose() { }

		public override TimeSpan TimerInterval { get { return m_timerInterval; } }		
		public IPluginData GetData() { return null; }

		// Temperature doesn't need the Output Handler
		public override void EventHandler(object sender, IPluginData data)
		{
			throw new System.NotImplementedException();
		}

		public override void TimerCallback(object state)
		{
            Debug.Print("DHTSensor Callback");
            DHTData _DHTData = new DHTData();
			// get current readings
            _DHTData.SetData(ReadSensor());

            Debug.Print("DHTSensor.AirTemp = " + (float)_DHTData.GetData()[0].Value);
            Debug.Print("DHTSensor.Humidity = " + (float)_DHTData.GetData()[1].Value);
			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
            ida(_DHTData);
		}

		/// <summary>
		/// Read DHT Data
		/// </summary>
		/// <returns>Float value of current Temperature reading</returns>
        private PluginData[] ReadSensor()
		{
            PluginData[] _PluginData = new PluginData[1];
            var dhtSensor = new Dht11Sensor(Pins.GPIO_PIN_D12, Pins.GPIO_PIN_D13, PullUpResistor.Internal);
            if(dhtSensor.Read())
            {
                //Debug.Print("DHT sensor Read() ok, RH = " + dhtSensor.Humidity.ToString("F1") + "%, Temp = " + dhtSensor.Temperature.ToString("F1") + "°C");
                _PluginData[0].Name = "Air Temperature";
                _PluginData[0].UnitOFMeasurment = "°C";
                _PluginData[0].Value = dhtSensor.Temperature;
                _PluginData[0].ThingSpeakFieldID = 6;

                _PluginData[0].Name = "Humidity";
                _PluginData[0].UnitOFMeasurment = "%";
                _PluginData[0].Value = dhtSensor.Humidity;
                _PluginData[0].ThingSpeakFieldID = 7;
            }
            else
            {
                Debug.Print("DHT sensor Read() failed");
            }
            return _PluginData;
		}
	}
}
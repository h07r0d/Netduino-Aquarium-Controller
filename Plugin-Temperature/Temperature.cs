using Controller;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Collections;
using System;
using Microsoft.SPOT.Hardware;
using ThreelnDotOrg.NETMF.Hardware;


namespace Plugins
{	
	public class TempData : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
	}

	/// <summary>
    /// DS18B20 Digital Temp Sensor: https://www.sparkfun.com/products/11050
	/// </summary>
	public class Temperature : InputPlugin
	{
		private TimeSpan m_timerInterval;
        public override bool ImplimentsEventHandler() { return false; }


		public Temperature() { }		
		public Temperature(object _config) : base()
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

		~Temperature() { Dispose(); }
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
			TempData tempData = new TempData();
			// get current temperature
			tempData.SetData(CalculateTemperature());

            foreach (PluginData pd in tempData.GetData())
            {
                Debug.Print(pd.Name + " = " + pd.Value.ToString());
            }

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(tempData);
		}

		/// <summary>
		/// Obtain Temperature value
		/// </summary>
		/// <returns>PluginData Array</returns>
        /// <remarks>For use with the DS18B20 Temperature sensor</remarks>
		private PluginData[] CalculateTemperature()
		{
            PluginData[] _PluginData = new PluginData[1];
            _PluginData[0] = new PluginData();
            bool ReadSuccess = true;
            double temp = 0.0;
            try
            {
                DS18B20 t = new DS18B20(Pins.GPIO_PIN_A0);
                temp = t.ConvertAndReadTemperature();
                //float tempf = temp / 5 * 9 + 32;
                t.Dispose();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                ReadSuccess = false;
            }
            finally
            {
                _PluginData[0].Name = "Temperature";
                _PluginData[0].UnitOFMeasurment = "C";
                _PluginData[0].Value = temp;
                _PluginData[0].ThingSpeakFieldID = 1;
                _PluginData[0].LastReadSuccess = ReadSuccess;
            }

            return _PluginData;
		}
	}
}
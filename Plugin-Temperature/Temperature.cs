using Controller;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System;

namespace Plugins
{
	
	public class TemperatureData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
		public string DataUnits() { return "C"; }
		public ThingSpeakFields DataType() { return ThingSpeakFields.Temperature; }
	}

	/// <summary>
	/// LN35DZ precision Centigrade chip
	/// </summary>
	public class Temperature : IPlugin
	{
		~Temperature() { Dispose(); }
		public void Dispose() { }

		private TemperatureData m_data;
		private AnalogInput m_analogInput;

		public int TimerInterval() { return 60; }
		public Category Category() { return Controller.Category.Input; }
		public IPluginData GetData() { return m_data; }
		public void EventHandler(object sender, IPluginData data) { }

		public Temperature()
		{
			m_data = new TemperatureData();
			m_analogInput = new AnalogInput(Pins.GPIO_PIN_A0);
		}

		public void TimerCallback(object state)
		{
			Debug.Print("Temperature Callback Hit\n");
			// get current temperature
			m_data.SetValue(CalculateTemperature());

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(m_data);
		}

		/// <summary>
		/// Calculate Temperature value
		/// </summary>
		/// <returns>Float value of current Temperature reading</returns>
		private float CalculateTemperature()
		{
			// read analog pin and convert to celcius according to datasheet
			// assuming AREF of 3.3V
			int raw = m_analogInput.Read();
			Debug.Print(raw.ToString());
			float result = (1023.0f * raw) / 3.3f;
			Debug.Print(result.ToString("F"));
			return result;
		}
	}
}
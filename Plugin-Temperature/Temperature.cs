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
	public class Temperature : Plugin
	{
		~Temperature() { Dispose(); }
		public override void Dispose() { }

		private TemperatureData m_data;
		private AnalogInput m_analogInput;

		public override int TimerInterval() { return 60; }
		public override Category Category() { return Controller.Category.Input; }
		public IPluginData GetData() { return m_data; }
		public override void EventHandler(object sender, IPluginData data) { }

		public Temperature()
		{
			m_data = new TemperatureData();
			m_analogInput = new AnalogInput(Pins.GPIO_PIN_A0);
		}

		public override void TimerCallback(object state)
		{
			if (!Enabled()) return;
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
		/// <remarks>Assuming AREF of 3.3v, the default for Rev. B Netduino Plus boards.
		/// It's an internal value, no feed to AREF required.
		/// Pinouts for the probe:
		/// - Brown=Ground
		/// - White/Green=VIn
		/// - Green=AnalogRead</remarks>
		private float CalculateTemperature()
		{
			// take 10 readings to even out the voltage
			int voltage = 0;
			for (int i = 0; i < 10; i++) { voltage += m_analogInput.Read(); }
			voltage /= 10;
			
			return (3.3f * voltage * 100f) / 1023f;
		}
	}
}
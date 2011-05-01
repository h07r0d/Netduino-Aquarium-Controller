using System;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	public class TemperatureData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
		public string DataUnits() { return "C"; }
		public string DataType() { return "Temperature"; }
	}

	public class Temperature : IPlugin
	{
		private TemperatureData m_data = new TemperatureData();
		public int TimerInterval() { return 60; }
		public Category Category() { return Controller.Category.Input; }
		public IPluginData GetData() { return m_data; }
		public void EventHandler(object sender, IPluginData data) { }
		public void TimerCallback(object state)
		{
			Debug.Print("Temperature Callbackk Hit\n");
			// get current temperature
			m_data.SetValue(24.6f);

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(m_data);
		}
	}
}

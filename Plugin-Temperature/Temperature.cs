using System;
using Controller;
using Microsoft.SPOT;

namespace Plugin_Temperature
{
	public class TemperatureData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
	}

	public class Temperature : IPlugin
	{
		private TemperatureData m_data;
		public int PluginTimerInterval() { return 60; }
		public Category PluginCategory() { return Category.Input; }
		public IPluginData GetData() { return m_data; }
		public void PluginEventHandler(object sender, IPluginData data) { }
		public void PluginTimerCallback(object state)
		{
			// get current temperature
			m_data.SetValue(24.6f);

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(m_data);
		}
	}
}

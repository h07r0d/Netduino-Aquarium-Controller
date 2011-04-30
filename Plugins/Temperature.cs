using System;
using Microsoft.SPOT;
using System.Threading;

namespace Controller
{
	public delegate void InputDataAvailable(IPluginData _data);
	class TemperatureData : IPluginData
	{
		public int Temperature;
	}

	class Temperature : IPlugin
	{
		
		private TemperatureData m_data = new TemperatureData();
		public Category PluginCategory() { return Category.Input; }
		public void PluginEventHandler(object sender, IPluginData data) { }
		public IPluginData[] GetData() { return null; }
		public int PluginTimerInterval() { return 60; }

		public void PluginTimerCallback(object state)
		{
			var returns = (InputDataAvailable)state;
			
			// Poll data lines

			returns(m_data);

		}
	}
}

using System;
using Controller;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Plugins
{

	public class TemperatureData : IPluginData
	{
		private double m_value;
		public double GetValue() { return m_value; }
		public void SetValue(double _value) { m_value = _value; }
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
			Debug.Print("Temperature Callback Hit\n");
			// get current temperature
			m_data.SetValue(CalculateTemperature());

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(m_data);
		}

		private double CalculateTemperature()
		{
			// pulled from http://www.arduino.cc/playground/ComponentLib/Thermistor2
			AnalogInput temp_in;
			double temp=0;
			double temp_out=0;

			// run this 10 times and average to ensure we're accurate
			for (int i = 0; i < 10; i++)
			{
				temp_in = new AnalogInput(Pins.GPIO_PIN_A0);				
				temp = Log(((10240000 / temp_in.Read()) - 10000));
				temp = 1 / (0.001129148 + (0.000234125 + (0.0000000876741 * temp * temp)) * temp);
				temp_out = temp - 273.15;            // Convert Kelvin to Celcius
			}
			return temp_out/10; 
		}


		public static double Log(double x)
		{
			// Based on Python sourcecode from:
			// http://en.literateprograms.org/Logarithm_Function_%28Python%29

			double partial = 0.5F;
			double integer = 0F;
			double fractional = 0.0F;
			double newBase = 10F;

			if (x == 0.0F) return double.NegativeInfinity;
			if ((x < 1.0F) & (newBase < 1.0F)) throw new ArgumentOutOfRangeException("can't compute Log");

			while (x < 1.0F)
			{
				integer -= 1F;
				x *= newBase;
			}

			while (x >= newBase)
			{
				integer += 1F;
				x /= newBase;
			}

			x *= x;

			while (partial >= double.Epsilon)
			{
				if (x >= newBase)
				{
					fractional += partial;
					x = x / newBase;
				}
				partial *= 0.5F;
				x *= x;
			}

			return (integer + fractional);
		}
	}
}

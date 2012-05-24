using Controller;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;


namespace Plugins
{	
	public class TempData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
		public string DataUnits() { return "C"; }
		public ThingSpeakFields DataType() { return ThingSpeakFields.Temperature; }
	}

	/// <summary>
	/// 10K Thermistor, available at http://www.adafruit.com/products/372
	/// </summary>
	public class Temperature : InputPlugin
	{
		private const int SeriesResistor = 10000;
		private const int ThermistorNominal = 10000;
		private const int TemperatureNominal = 25;
		private const int BetaCoefficient = 3950;

		public Temperature() { }
		public Temperature(object _config) : base() { }
		~Temperature() { Dispose(); }
		public override void Dispose() { }

		public override int TimerInterval { get { return 2; } }		
		public IPluginData GetData() { return null; }

		public override void TimerCallback(object state)
		{
			Debug.Print("Temperature Callback");
			TempData tempData = new TempData();
			// get current temperature
			tempData.SetValue(CalculateTemperature());

			Debug.Print("Temperature = "+tempData.GetValue().ToString());
			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(tempData);
		}

		/// <summary>
		/// Calculate Temperature value
		/// </summary>
		/// <returns>Float value of current Temperature reading</returns>
		/// <remarks>Assuming AREF of 3.3v, the default for Rev. B Netduino Plus boards.
		/// It's an internal value, no feed to AREF required.
		/// Using code tutorial from adafruit http://www.ladyada.net/learn/sensors/thermistor.html </remarks>
		private float CalculateTemperature()
		{
			AnalogInput ain = new AnalogInput(Pins.GPIO_PIN_A0);			

			// take 10 readings to even out the noise
			float average = 0.0F;
			for (int i = 0; i < 10; i++) { average += ain.Read(); }
			average /= 10;
			
			// convert to a resistance
			average = 1023 / average - 1;
			average = SeriesResistor / average;

			// apply steinhart
			float tempValue = average / ThermistorNominal;
			tempValue = Controller.Math.Log(tempValue);
			tempValue /= BetaCoefficient;
			tempValue += 1.0F / (TemperatureNominal + 273.15F);
			tempValue = 1.0F / tempValue;
			tempValue -= 273.15F;
			
			ain.Dispose();
			return tempValue;
		}
	}
}
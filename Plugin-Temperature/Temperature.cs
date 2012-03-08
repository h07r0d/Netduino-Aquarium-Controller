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
	/// LM35DZ precision Centigrade chip
	/// </summary>
	public class Temperature : InputPlugin
	{
		~Temperature() { Dispose(); }
		public override void Dispose() { }

		private TempData m_data;
		private AnalogInput m_analogInput;

		public override int TimerInterval { get { return 15; } }
		public override string WebFragment { get { return "temperature.html"; } }
		public IPluginData GetData() { return m_data; }

		public Temperature()
		{
			m_data = new TempData();
			m_analogInput = new AnalogInput(Pins.GPIO_PIN_A0);
		}

		public Temperature(object _config) : base() { }

		public override void TimerCallback(object state)
		{
			Debug.Print("Temperature Callback");
			// get current temperature
			m_data.SetValue(CalculateTemperature());

			Debug.Print(m_data.GetValue().ToString());
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
		/// It's an internal value, no feed to AREF required.</remarks>
		private float CalculateTemperature()
		{
			// take 10 readings to even out the voltage
			int voltage = 0;
			for (int i = 0; i < 10; i++) { voltage += m_analogInput.Read();
			Debug.Print(voltage.ToString());}
			voltage /= 10;
			
			// Amplifier circuit in place, pumping up the millivolt readings to volts
			// Also, the amplifier circuit introduces some drift, so adding a 'fudge' factor
			// to compensate.  This is purely from emperical data gathered
			float tempValue = ((3.3f * voltage * 10f) / 1023f) - 1.4f;
			return tempValue;
		}
	}
}
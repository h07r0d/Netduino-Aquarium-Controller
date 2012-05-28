using Controller;
using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Plugins
{

	public class AlkalinityData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
		public string DataUnits() { return "pH"; }
		public ThingSpeakFields DataType() { return ThingSpeakFields.pH; }
	}

	/// <summary>
	/// pH input data using the pH kit from Atlast Scientific
	/// http://atlas-scientific.com/product_pages/kits/ph-kit.html
	/// The pH stamp provides simple UART based serial communication for data.
	/// It also provides temperature compensation, so the pH plugin has both an
	/// Input Callback and and Output Callback, to allow the temperature input
	/// plugin to update the pH plugin.
	/// </summary>
	public class pH : InputPlugin
	{
		
		public override int TimerInterval { get { return 2; } }
		public IPluginData GetData() { return null; }

		/// <summary>
		/// Latest temperature reading passed in from the Temperature Plugin
		/// </summary>
		private float m_Temperature;

		public pH()
		{						
			m_Temperature = 0.0F;
		}

		public pH(object _config) : base() { }

		~pH() { Dispose(); }
		public override void Dispose() { }

		/// <summary>
		/// Records the last temperature reading from the Temperature Plugin
		/// </summary>
		/// <param name="_sender">Object that raised the callback</param>
		/// <param name="_data">Last reading</param>
		public void EventHandler(object _sender, IPluginData _data)
		{
			// Only worry about Temperature data, so check data units.
			// If it's 'C' then assume it's the one we want.
			if (_data.DataUnits().Equals("C")) m_Temperature = _data.GetValue();
		}

		public override void TimerCallback(object state)
		{
			Debug.Print("pH Callback");			
			AlkalinityData phData = new AlkalinityData();

			// get current pH Value			
			phData.SetValue(CalculatePH());
			Debug.Print("pH = " + phData.GetValue().ToString("F"));			

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			ida(phData);
		}

		/// <summary>
		/// Takes reading from Atlas Scientific pH Stamp
		/// </summary>
		/// <returns></returns>
		private float CalculatePH()
		{
			try
			{
				string command = "";
				// Send the temperature reading if available
				if (m_Temperature > 0)
				{
					Debug.Print("Temp value available");
					// send temperature, and ask for a single reading
					command = m_Temperature.ToString("F") + "\rR\r";
				}
				else
				{
					command = "R\r";
				}

				object locker = new object();
				string response = "";
				char inChar;
				byte[] message = Encoding.UTF8.GetBytes(command);
				
				lock (locker)
				{
					SerialPort sp = new SerialPort(Serial.COM1, 38400, Parity.None, 8, StopBits.One);
					sp.Open();
					sp.Write(message, 0, message.Length);
					Debug.Print("sending message");

					// Now collect response
					while ((inChar = (char)sp.ReadByte()) != '\r') { response += inChar; Debug.Print(response); }
					sp.Close();
				}
				// Stamp can return text if reading was not successful, so test before returning
				double phReading;
				if (Double.TryParse(response, out phReading)) { Debug.Print(phReading.ToString()); return (float)phReading; }
				else return 0.0F;
			}
			catch (Exception e)
			{
				Debug.Print(e.StackTrace);
			}
			return 0.0F;
		}
	}
}

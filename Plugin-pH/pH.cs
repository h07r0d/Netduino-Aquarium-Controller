using Controller;
using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Collections;

namespace Plugins
{

	public class AlkalinityData : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
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
		private TimeSpan m_timerInterval;
		public override TimeSpan TimerInterval { get { return m_timerInterval; } }
		
		/// <summary>
		/// Latest temperature reading passed in from the Temperature Plugin
		/// </summary>
		private float m_Temperature;

		public pH() { m_Temperature = 0.0F; }
		public pH(object _config) : base() 
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

		~pH() { Dispose(); }
		public override void Dispose() { }

		/// <summary>
		/// Records the last temperature reading from the Temperature Plugin
		/// </summary>
		/// <param name="_sender">Object that raised the callback</param>
		/// <param name="_data">Last reading</param>
		public override void EventHandler(object _sender, PluginData[] _data)
		{
			// Only worry about Temperature data, so check data units.
            // If it's 'C' and the Name = 'Temperature' then assume it's the one we want.
			Debug.Print("Got Temperature Value");
            foreach (PluginData _pd in _data)
            {
                if (_pd.Name.Equals("Temperature") & _pd.UnitOFMeasurment.Equals("C")) m_Temperature = (float)_pd.Value;
            }
		}

		public override void TimerCallback(object state)
		{
			Debug.Print("pH Callback");			
			AlkalinityData phData = new AlkalinityData();

			// get current pH Value			
			phData.SetData(CalculatePH());
			Debug.Print("pH = " + (float)phData.GetData()[0].Value);			

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			// TODO: Currently there is a glitch with SerialPort and
			// sometimes data doesn't come back from the Stamp.
			// Discard bad readings, and report any meaningful ones
			if ((float)phData.GetData()[0].Value > 0.0F) ida(phData);
		}

		/// <summary>
		/// Takes reading from Atlas Scientific pH Stamp
		/// </summary>
		/// <returns></returns>
		private PluginData[] CalculatePH()
		{
			float ph = 0.0F;
            PluginData[] _PluginData = new PluginData[0];
			SerialPort sp = new SerialPort(Serial.COM1, 38400, Parity.None, 8, StopBits.One);
			sp.ReadTimeout = 1000;

			try
			{
				string command = "";
				string response = "";
				char inChar;

				// Send the temperature reading if available
				if (m_Temperature > 0)
					command = m_Temperature.ToString("F") + "\rR\r";
				else
					command = "R\r";

				Debug.Print(command);
				byte[] message = Encoding.UTF8.GetBytes(command);

				sp.Open();
				sp.Write(message, 0, message.Length);
				sp.Flush();
				Debug.Print("sending message");

				// Now collect response
				while ((inChar = (char)sp.ReadByte()) != '\r') { response += inChar; }
				
				// Stamp can return text if reading was not successful, so test before returning
				double phReading;
				if (Double.TryParse(response, out phReading)) ph = (float)phReading;

                PluginData[] _Data = new PluginData[2];

                _PluginData[0].Name = "Alkalinity";
                _PluginData[0].UnitOFMeasurment = "pH";
                _PluginData[0].Value = ph;
                _PluginData[0].ThingSpeakFieldID = 2;
			}
			catch (Exception e)
			{
				Debug.Print(e.StackTrace);
			}
			finally
			{
				sp.Close();
				sp.Dispose();
			}
            return _PluginData;
		}
	}
}
using Controller;
using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Collections;

namespace Plugins
{

    public class ElectricalConductivityData : IPluginData
	{
		private float m_value;
		public float GetValue() { return m_value; }
		public void SetValue(float _value) { m_value = _value; }
		public string DataUnits() { return "ppm"; }
		public ThingSpeakFields DataType() { return ThingSpeakFields.PPM; }
	}

	/// <summary>
	/// ElectricalConductivity input data using the ElectricalConductivity kit from Atlast Scientific
    /// http://atlas-scientific.com/product_pages/kits/ec-kit.html
    /// The ElectricalConductivity stamp provides simple UART based serial communication for data.
    /// It also provides temperature compensation, so the EC plugin has both an
    /// Input Callback and and Output Callback, to allow the temperature input
    /// plugin to update the EC plugin.
	/// </summary>

	public class ElectricalConductivity : InputPlugin
	{
		private TimeSpan m_timerInterval;
		public override TimeSpan TimerInterval { get { return m_timerInterval; } }

        /// <summary>
        /// Latest temperature reading passed in from the Temperature Plugin
        /// </summary>
        private float m_Temperature;

        public ElectricalConductivity() { m_Temperature = 0.0F; }
		public ElectricalConductivity(object _config) : base() 
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

		~ElectricalConductivity() { Dispose(); }
		public override void Dispose() { }

        /// <summary>
        /// Records the last temperature reading from the Temperature Plugin
        /// </summary>
        /// <param name="_sender">Object that raised the callback</param>
        /// <param name="_data">Last reading</param>
        public override void EventHandler(object _sender, IPluginData _data)
        {
            // Only worry about Temperature data, so check data units.
            // If it's 'C' then assume it's the one we want.
            Debug.Print("Got Temperature Value");
            if (_data.DataUnits().Equals("C")) m_Temperature = _data.GetValue();
        }

		public override void TimerCallback(object state)
		{
			Debug.Print("ElectricalConductivity Callback");
            ElectricalConductivityData ECData = new ElectricalConductivityData();

			// get current ElectricalConductivity Value			
            ECData.SetValue(GetPPM());
            Debug.Print("ElectricalConductivity = " + ECData.GetValue().ToString("F"));			

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			// TODO: Currently there is a glitch with SerialPort and
			// sometimes data doesn't come back from the Stamp.
			// Discard bad readings, and report any meaningful ones
            if (ECData.GetValue() > 0.0F) ida(ECData);
		}

		/// <summary>
		/// Takes reading from Atlas Scientific ElectricalConductivity Stamp
		/// </summary>
		/// <returns></returns>
		private float GetPPM()
		{
			float PPM = 0.0F;
			SerialPort sp = new SerialPort(Serial.COM2, 38400, Parity.None, 8, StopBits.One);
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

                response = response.Split(',')[1];
                
				// Stamp can return text if reading was not successful, so test before returning
				double ppmReading;
                if (Double.TryParse(response, out ppmReading)) PPM = (float)ppmReading;
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
			return PPM;
		}
	}
}
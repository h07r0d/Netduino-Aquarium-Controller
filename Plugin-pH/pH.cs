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
        public override bool ImplimentsEventHandler() { return true; }

		/// <summary>
		/// Latest temperature reading passed in from the Temperature Plugin
		/// </summary>
		private PluginData m_Temperature = new PluginData();

        public pH() { }
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
        public override void EventHandler(object _sender, IPluginData _data)
        {
            // Only worry about Temperature data, so check data units.
            // If it's 'C' and the Name = 'Temperature' then assume it's the one we want.
            foreach (PluginData _pd in _data.GetData())
            {
                if (_pd.Name.Equals("Temperature") && _pd.UnitOFMeasurment.Equals("C") && _pd.LastReadSuccess)
                {
                    Debug.Print("PH Plugin Got Temperature Value");
                    m_Temperature = _pd;
                }
            }
        }

		public override void TimerCallback(object state)
		{
			AlkalinityData phData = new AlkalinityData();

			// get current pH Value			
			phData.SetData(CalculatePH());

            foreach (PluginData pd in phData.GetData())
            {
                Debug.Print(pd.Name + " = " + pd.Value.ToString("F"));
            }

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			// TODO: Currently there is a glitch with SerialPort and
			// sometimes data doesn't come back from the Stamp.
			// Discard bad readings, and report any meaningful ones
			//if ((float)phData.GetData()[0].Value > 0.0F) 
            ida(phData);
		}

		/// <summary>
		/// Takes reading from Atlas Scientific pH Stamp
		/// </summary>
		/// <returns></returns>
		private PluginData[] CalculatePH()
		{
			double ph = 0.0;
            PluginData[] _PluginData = new PluginData[1];
            _PluginData[0] = new PluginData();
			SerialPort sp = new SerialPort(Serial.COM2, 38400, Parity.None, 8, StopBits.One);
			sp.ReadTimeout = 6000;
            sp.WriteTimeout = 4000;
            bool ReadSuccess = true;

			try
			{
				string command = "";
				string response = "";
				char inChar;

				// Send the temperature reading if available
				if (m_Temperature.LastReadSuccess)
					command = "\r" + m_Temperature.Value.ToString("F") + "R\r";
				else
					command = "\rR\r";

				byte[] message = Encoding.UTF8.GetBytes(command);

                Debug.Print("sending message");
				sp.Open();
				sp.Write(message, 0, message.Length);
				sp.Flush();
                Thread.Sleep(2000);

				// Now collect response
                try
                {
                    while (sp.BytesToRead > 0)
                    {
                        inChar = (char)sp.ReadByte();
                        if (inChar != '\r' && inChar != '\0')
                            response += inChar;
                    }
                    Debug.Print("Response:" + response);
                }
                catch (Exception e)
                { 
                    Debug.Print("Could not read from the PH stamp.  Please check the connection.");
                    ReadSuccess = false;
                }
				
				// Stamp can return text if reading was not successful, so test before returning
				double phReading;
				if (Double.TryParse(response, out phReading)) ph = (float)phReading;
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
                ReadSuccess = false;
			}
			finally
			{
                _PluginData[0].Name = "pH";
                _PluginData[0].UnitOFMeasurment = "pH";
                _PluginData[0].Value = ph;
                _PluginData[0].ThingSpeakFieldID = 2;
                _PluginData[0].LastReadSuccess = ReadSuccess;

				if (sp.IsOpen) sp.Close();
				sp.Dispose();
			}
            return _PluginData;
		}
	}
}
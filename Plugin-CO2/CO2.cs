using Controller;
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Collections;

namespace Plugins
{	
	public class COIIData : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
	}

	/// <summary>
	/// CO2 Sensor Available at:
    /// http://sandboxelectronics.com/store/index.php?main_page=product_info&cPath=66&products_id=197
    /// This sensor is connected to an arduono.  See the ArduinoBridge code to see how it handles averything.
	/// </summary>
	public class CO2 : InputPlugin
	{
		private TimeSpan m_timerInterval;
        public override bool ImplimentsEventHandler() { return false; }
        public CO2() { }		
		public CO2(object _config) : base()
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

		~CO2() { Dispose(); }
		public override void Dispose() { }

		public override TimeSpan TimerInterval { get { return m_timerInterval; } }		
		public IPluginData GetData() { return null; }

		// Temperature doesn't need the Output Handler
		public override void EventHandler(object sender, IPluginData data)
		{
			throw new System.NotImplementedException();
		}

		public override void TimerCallback(object state)
		{
			COIIData _CO2Data = new COIIData();
			// get current temperature
			_CO2Data.SetData(ReadSensor());

            foreach (PluginData pd in _CO2Data.GetData())
            {
                Debug.Print(pd.Name + " = " + pd.Value.ToString("F"));
            }
			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
            ida(_CO2Data);
		}

		/// <summary>
		/// Obtain CO2 value
		/// </summary>
		/// <returns>PluginData Array</returns>
        /// <remarks>For use with the MG-811 CO2 Sensor and arduino bridge</remarks>
		private PluginData[] ReadSensor()
		{
            double CO2 = 0.0;
            bool ReadSuccess = true;
            PluginData[] _PluginData = new PluginData[1];
            _PluginData[0] = new PluginData();
            SerialPort sp = new SerialPort(SecretLabs.NETMF.Hardware.Netduino.SerialPorts.COM4, 57600, Parity.None, 8, StopBits.One);
            sp.ReadTimeout = 3000;
            sp.WriteTimeout = 3000;

            try
            {
                string command = "";
                string response = "";
                char inChar;

                command = "CO2\n";

                byte[] message = Encoding.UTF8.GetBytes(command);

                sp.Open();
                sp.Write(message, 0, message.Length);
                sp.Flush();
                Thread.Sleep(1000);

                // Now collect response
                try
                {
                    while (sp.BytesToRead > 0)
                    {
                        inChar = (char)sp.ReadByte();
                        if (inChar != '\r' && inChar != '\0')
                            response += inChar;
                    }
                    Debug.Print("CO2 Stamp Response:" + response);
                }
                catch (Exception e)
                {
                    Debug.Print("Could not read from CO2 Stamp.  Please check connection.");
                    ReadSuccess = false;
                }

                if (response.Length > 0)
                {
                    CO2 = double.Parse(response);
                }
                else ReadSuccess = false;

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                ReadSuccess = false;
            }
            finally
            {
                _PluginData[0].Name = "CO2";
                _PluginData[0].UnitOFMeasurment = "PPM";
                _PluginData[0].Value = CO2;
                _PluginData[0].ThingSpeakFieldID = 3;
                _PluginData[0].LastReadSuccess = ReadSuccess;

                if(sp.IsOpen)
                    sp.Close();
                sp.Dispose();
            }
            return _PluginData;
		}
	}
}
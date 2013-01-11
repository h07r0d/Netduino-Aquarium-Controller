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
	public class HT : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
	}

	/// <summary>
	/// Plugin for DHT11 Temperature and Humidity sensor.
    /// This sensor is connected to the arduino bridge because I couldn't
    /// get the bit banger code for this device to work properly.
	/// </summary>
	public class DHTSensor : InputPlugin
	{
        public override bool ImplimentsEventHandler() { return false; }
		private TimeSpan m_timerInterval;
        public IPluginData GetData() { return null; }

		public DHTSensor() { }
		public DHTSensor(object _config) : base()
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

        ~DHTSensor() { Dispose(); }
		public override void Dispose() { }

		public override TimeSpan TimerInterval { get { return m_timerInterval; } }		

		// Temperature doesn't need the Output Handler
		public override void EventHandler(object sender, IPluginData data)
		{
			throw new System.NotImplementedException();
		}

		public override void TimerCallback(object state)
		{
            Debug.Print("DHTSensor Callback");
            HT _DHTData = new HT();
			// get current readings
            _DHTData.SetData(ReadSensor());

            foreach (PluginData pd in _DHTData.GetData())
            {
                Debug.Print(pd.Name + " = " + pd.Value.ToString("F"));
            }
			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
            ida(_DHTData);
		}

		/// <summary>
		/// Read DHT Data
		/// </summary>
		/// <returns>Float value of current Temperature reading</returns>
        private PluginData[] ReadSensor()
		{
            PluginData[] _PluginData = new PluginData[2];
            _PluginData[0] = new PluginData();
            _PluginData[1] = new PluginData();
            Double Temp = 0;
            Double Humidity = 0;
            bool ReadSuccess = true;

            SerialPort sp = new SerialPort(SecretLabs.NETMF.Hardware.Netduino.SerialPorts.COM4, 57600, Parity.None, 8, StopBits.One);
			sp.ReadTimeout = 3000;
            sp.WriteTimeout = 3000;

            try
            {
                string command = "";
                string response = "";
                char inChar;
                
                command = "R\n";

                byte[] message = Encoding.UTF8.GetBytes(command);

				sp.Open();
				sp.Write(message, 0, message.Length);
				sp.Flush();
				//Debug.Print("Sending \"" + command + "\" to the DHT Bridge");
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
                    Debug.Print("DHT Response:" + response);
                }
                catch(Exception e)
                { 
                    Debug.Print("Could not read from DHT Stamp.  Please check connection.");
                    ReadSuccess = false;
                }

                if (response.Length > 0)
                {
                    string[] _split;
                    _split = response.Split(',');
                    Temp = double.Parse(_split[0]);
                    Humidity = double.Parse(_split[1]);
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
                //Assign the response to a PluginData Variable

                for (int i=0;i< _PluginData.Length;i++)
                    _PluginData[i].LastReadSuccess = ReadSuccess;

                _PluginData[0].Value = Temp;
                _PluginData[0].Name = "AirTemperature";
                _PluginData[0].UnitOFMeasurment = "C";
                _PluginData[0].ThingSpeakFieldID = 7;

                _PluginData[1].Value = Humidity;
                _PluginData[1].Name = "Humidity";
                _PluginData[1].UnitOFMeasurment = "%";
                _PluginData[1].ThingSpeakFieldID = 8;

                if (sp.IsOpen)
                    sp.Close();
                sp.Dispose();
            }
            return _PluginData;
		}
	}
}
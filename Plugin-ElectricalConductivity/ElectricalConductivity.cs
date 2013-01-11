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

    public class ECD : IPluginData
	{
        private PluginData[] _PluginData;
        public PluginData[] GetData() { return _PluginData; }
        public void SetData(PluginData[] _value) { _PluginData = _value; }
	}

	/// <summary>
	/// ElectricalConductivity input data using the ElectricalConductivity kit from Atlast Scientific
    /// http://atlas-scientific.com/product_pages/kits/ec-kit.html
    /// The ElectricalConductivity stamp provides simple UART based serial communication for data.
    /// It also provides temperature compensation, so the EC plugin has both an
    /// Input Callback and and Output Callback, to allow the temperature input
    /// plugin to update the EC plugin.
    /// <remarks>
    /// If you are using this in conjunction with Atlas' 
    /// PH meter, you need to put an N-Channel Mosfet in line with the probe's ground cable
    /// to prevent interfering with the PH meter.
    /// and switch on when taking a reading.. then back off when done.
    /// </remarks>
	/// </summary>

	public class ElectricalConductivity : InputPlugin
	{
		private TimeSpan m_timerInterval;
        private int m_probetype;
        public override bool ImplimentsEventHandler() { return true; }
		public override TimeSpan TimerInterval { get { return m_timerInterval; } }

        /// <summary>
        /// Latest temperature reading passed in from the Temperature Plugin
        /// </summary>
        private PluginData m_Temperature = new PluginData();

        public ElectricalConductivity() { }
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

            //Set Probe Type
            //m_probetype = int.Parse(config["ProbeType"].ToString());
            //SetProbeType();
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
            // Only worry about Temperature data, so check data units and name.
            // If it's 'C' and 'Temperature' then assume it's the one we want.
            foreach (PluginData _pd in _data.GetData())
            {
                if (_pd.Name == "Temperature" && _pd.UnitOFMeasurment == "C" && _pd.LastReadSuccess)
                {
                    Debug.Print("EC Stamp Got Temperature Value");
                    m_Temperature = _pd;
                }
            }
        }

		public override void TimerCallback(object state)
		{
			Debug.Print("ElectricalConductivity Callback");
            ECD ECData = new ECD();

			// get current ElectricalConductivity Value			
            ECData.SetData(GetDataFromSensor());
            foreach (PluginData pd in ECData.GetData())
            {
                Debug.Print(pd.Name + " = " + pd.Value.ToString());
            }

			//Timer Callbacks receive a Delegate in the state object
			InputDataAvailable ida = (InputDataAvailable)state;

			// call out to the delegate with expected value
			// TODO: Currently there is a glitch with SerialPort and
			// sometimes data doesn't come back from the Stamp.
			// Discard bad readings, and report any meaningful ones
            //if (((float)ECData.GetData()[0].Value) > 0.0F) 
            ida(ECData);
		}

		/// <summary>
		/// Takes reading from Atlas Scientific ElectricalConductivity Stamp
		/// </summary>
		/// <returns></returns>
		private PluginData[] GetDataFromSensor()
		{
            PluginData[] _Data = new PluginData[3];
            _Data[0] = new PluginData();
            _Data[1] = new PluginData();
            _Data[2] = new PluginData();

			SerialPort sp = new SerialPort(Serial.COM1, 38400, Parity.None, 8, StopBits.One);
			sp.ReadTimeout = 4000;
            sp.WriteTimeout = 4000;
            double Microsiemens = 0;
            double TDS = 0;
            double Salinity = 0;
            bool MSReadSuccess = true;
            bool TDSReadSuccess = true;
            bool SReadSuccess = true;

            try
            {
                string command = "";
                string response = "";
                char inChar;
                
                // Send the temperature reading if available
                if (m_Temperature.LastReadSuccess)
                    command = '\r' + m_Temperature.Value.ToString("F") + "\r";
                else
                    command = "\rR\r";

                byte[] message = Encoding.UTF8.GetBytes(command);

				sp.Open();
				sp.Write(message, 0, message.Length);
				sp.Flush();
				Debug.Print("Sending \"" + command + "\" to the EC stamp");
                Thread.Sleep(3000);

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
                catch(Exception e)
                { 
                    Debug.Print("Could not read from EC Stamp.  Please check connection.");
                    MSReadSuccess = false;
                    TDSReadSuccess = false;
                    SReadSuccess = false;
                }

                if (response.Length > 0)
                {
                    string[] _split;
                    _split = response.Split(',');
                    try { Microsiemens = double.Parse(_split[0]); } 
                    catch (Exception e) { MSReadSuccess = false; }
                    try { TDS = double.Parse(_split[1]); }
                    catch (Exception e) { TDSReadSuccess = false; }
                    try { Salinity = double.Parse(_split[2]); }
                    catch (Exception e) { SReadSuccess = false; }
                }
                else
                {
                    MSReadSuccess = false;
                    TDSReadSuccess = false;
                    SReadSuccess = false;
                }

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                MSReadSuccess = false;
                TDSReadSuccess = false;
                SReadSuccess = false;
            }
            finally
            {
                //Assign the response to a PluginData Variable
                _Data[0].Name = "Microsiemens";
                _Data[0].UnitOFMeasurment = "µs";
                _Data[0].Value = Microsiemens;
                _Data[0].ThingSpeakFieldID = 4;
                _Data[0].LastReadSuccess = MSReadSuccess;

                _Data[1].Name = "TDS";
                _Data[1].UnitOFMeasurment = "PPM";
                _Data[1].Value = TDS;
                _Data[1].ThingSpeakFieldID = 5;
                _Data[1].LastReadSuccess = TDSReadSuccess;

                _Data[2].Name = "Salinity";
                _Data[2].UnitOFMeasurment = "Salinity";
                _Data[2].Value = Salinity;
                _Data[2].ThingSpeakFieldID = 6;
                _Data[2].LastReadSuccess = SReadSuccess;

				if(sp.IsOpen) sp.Close();
				sp.Dispose();
			}
            return _Data;
		}

        //private void SetProbeType()
        //{
        //    SerialPort sp = new SerialPort(Serial.COM1, 38400, Parity.None, 8, StopBits.One);
        //    sp.ReadTimeout = 10000;
        //    try
        //    {
        //        string command = "\rP," + m_probetype.ToString() + "\r";
        //        string response = "";
        //        char inChar;

        //        byte[] message = Encoding.UTF8.GetBytes(command);

        //        sp.Open();
        //        sp.Write(message, 0, message.Length);
        //        sp.Flush();
        //        Debug.Print("Sending \"" + command + "\" to the EC stamp");
        //        Thread.Sleep(2000);

        //        // Now collect response
        //        try
        //        {
        //            while (sp.BytesToRead > 0)
        //            {
        //                inChar = (char)sp.ReadByte();
        //                if (inChar != '\r' && inChar != '\0')
        //                    response += inChar;
        //            }
        //            Debug.Print("Response:" + response);
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.Print("Could not set EC Probe.  Please check connection.");
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Print(e.Message);
        //    }
        //    finally
        //    {
        //        if (sp.IsOpen) sp.Close();
        //        sp.Dispose();
        //    }
        //}
    }
}
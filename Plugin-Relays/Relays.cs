using System;
using System.Collections;
using Controller;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Plugins
{
	/// <summary>
	/// Control Plugin to manage the RelayShield http://www.seeedstudio.com/depot/relay-shield-p-693.html?cPath=132_134
	/// Used to control lighting and ATO solenoid
	/// </summary>
	public class Relays : ControlPlugin
	{
		~Relays() { Dispose(); }
		public override void Dispose() { m_relayPins = null; }

		/// <summary>
		/// Used during Command parsing to trigger immediate command execution.
		/// Example: during mid-day, the lights should be on, but a reboot of the controller will restart
		/// the Netduino, forcing a re-parse of the ini file.  
		/// The timers will be setup to turn on the next day, but they need to be on immediately as well
		/// </summary>
		private bool m_missedExecute = false;

		private OutputPort[] m_relayPins;
		public Relays()
		{
			m_relayPins = new OutputPort[4];
			m_relayPins[0] = new OutputPort(Pins.GPIO_PIN_D7, false);
			m_relayPins[1] = new OutputPort(Pins.GPIO_PIN_D6, false);
			m_relayPins[2] = new OutputPort(Pins.GPIO_PIN_D5, false);
			m_relayPins[3] = new OutputPort(Pins.GPIO_PIN_D4, false);
		}

		public override void ExecuteControl(object state)
		{
			// Callback received an ArrayList of RelayCommand structs to execute
			// Assign each command via array index, and write the pin value in status
			ArrayList commands = (ArrayList)state;
			foreach (RelayCommand item in commands)
			{
				m_relayPins[item.relay].Write(item.status);
			}
		}

		public override DictionaryEntry ParseCommand(string _time, string _command)
		{
			TimeSpan timespan = GetTimeSpan(_time.Substring(4,4));
			ArrayList arrayList = RelayCommands(_command);

			// handle required execution
			if (m_missedExecute)
			{
				ExecuteControl(arrayList);
				m_missedExecute = false;
			}

			return new DictionaryEntry(timespan, arrayList);
		}

		/// <summary>
		/// Determine the timespan from 'now' when the given command should be run
		/// </summary>
		/// <param name="_time">4 digit string representing a 24-hour time</param>
		/// <returns>Timespan when task should be run</returns>		
		private TimeSpan GetTimeSpan(string _time)
		{
			// Determine when this event should be fired as a TimeSpan
			// Assuming the hour mark is at "##00" in the time string and
			// minutes are at "00##" in the time string			
			int hours = int.Parse(_time.Substring(0, 2));
			int minutes = int.Parse(_time.Substring(2, 2));
			DateTime now = DateTime.Now;
			DateTime timeToRun = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

			// we missed the window, so setup for next run
			// Also, mark that we missed the run, so we can execute the command during the parse.
			// This fixes issues
			if (timeToRun < now)
			{
				timeToRun = timeToRun.AddDays(1);
				m_missedExecute = true;
			}

			return new TimeSpan((timeToRun - now).Ticks);
		}

		/// <summary>
		/// Create list of Relay Commands
		/// </summary>
		/// <param name="_commands">formatted command string that needs to be parsed</param>
		/// <returns>Array of <see cref="RelayCommand"/> objects</returns>
		private ArrayList RelayCommands(string _commands)
		{
			const int GET_RELAY = 0;
			const int GET_STATE = 1;			
			int state = GET_RELAY;
			string value = string.Empty;
			ArrayList commands = new ArrayList();
			RelayCommand rc = new RelayCommand();
			for (int i = 0; i < _commands.Length; i++)
			{
				switch (state)
				{
					case GET_RELAY:
						if (_commands[i] == ',')
							state++;
						else
							rc.relay = (_commands[i]-48);	// Convert to int value
						break;
					case GET_STATE:
						if (_commands[i] == '|')	// End of command, store struct
						{							
							switch (value)
							{
								case "on":
									rc.status = true;
									break;
								case "off":
									rc.status = false;
									break;
								default:
									break;
							}
							commands.Add(rc);							
							value = string.Empty;
							state = GET_RELAY;
							rc = new RelayCommand();
							break;
						}
						else
							value += _commands[i];
						break;					
						
					default:
						break;
				}
			}

			// For loop done, add a command if there is a relay number set
			if (rc.relay >= 0)
			{
				switch (value)
				{
					case "on":
						rc.status = true;
						break;
					case "off":
						rc.status = false;
						break;
					default:
						break;
				}
				commands.Add(rc);
			}
			foreach (RelayCommand item in commands)
			{
				Debug.Print("Relay - " +item.relay.ToString()+":"+item.status.ToString());
			}

			return commands;
		}
	}
}

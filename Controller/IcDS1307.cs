using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Extensions;

namespace Controller {
	/// <summary>
	/// This class implements a complete driver for the Dallas Semiconductors / Maxim DS1307 I2C real-time clock: http://pdfserv.maxim-ic.com/en/ds/DS1307.pdf
	/// </summary>
	public class DS1307 : IDisposable
	{
		[Flags]
		// Defines the frequency of the signal on the SQW interrupt pin on the clock when enabled
		public enum SQWFreq { SQW_1Hz, SQW_4kHz, SQW_8kHz, SQW_32kHz, SQW_OFF };

		[Flags]
		// Defines the logic level on the SQW pin when the frequency is disabled
		public enum SQWDisabledOutputControl { Zero, One };

		// Real time clock I2C address
		public const int DS1307_I2C_ADDRESS = 0x68;
		// I2C bus frequency for the clock
		public const int DS1307_I2C_CLOCK_RATE_KHZ = 100;
		
		// Allow 10ms timeouts on all I2C transactions
		public const int DS1307_I2C_TRANSACTION_TIMEOUT_MS = 10;
		
		// Start / End addresses of the date/time registers
		public const byte DS1307_RTC_START_ADDRESS = 0x00;
		public const byte DS1307_RTC_END_ADDRESS = 0x06;
		public const byte DS1307_RTC_SIZE = 7;

		// Square wave frequency generator register address
		public const byte DS1307_SQUARE_WAVE_CTRL_REGISTER_ADDRESS = 0x07;

		// Start / End addresses of the user RAM registers
		public const byte DS1307_RAM_START_ADDRESS = 0x08;
		public const byte DS1307_RAM_END_ADDRESS = 0x3f;

		// Total size of the user RAM block
		public const byte DS1307_RAM_SIZE = 56;

		// Instance of the I2C clock
		readonly bool privateClock = false;
		readonly I2CDevice clock;
		readonly I2CDevice.Configuration config = new I2CDevice.Configuration(DS1307_I2C_ADDRESS, DS1307_I2C_CLOCK_RATE_KHZ);

		public DS1307() {
			privateClock = true;
			clock = new I2CDevice(config);
		}

		public DS1307(I2CDevice device) {
			clock = device;
		}

		/// <summary>
		/// Gets or Sets the current date / time.
		/// </summary>
		/// <returns>A DateTime object</returns>
		public DateTime CurrentDateTime {
			get {
				byte[] clockData = ReadRegister(DS1307_RTC_START_ADDRESS, DS1307_RTC_SIZE);
				return ParseDate(clockData);
			}
			set {
				WriteRegister(DS1307_RTC_START_ADDRESS, EncodeDate(value, ClockHalt, TwelveHourMode));
			}
		}
  
		private DateTime ParseDate(byte[] clockData) {
			int hour = 0;
			if ((clockData[0x02] & 0x40) == 0x40) { // 12-hour mode
				if ((hour & 0x20) == 0x20)
					hour += 12;
				hour += ((byte)(clockData[0x02] & 0x1f)).ToDecimal() + 1;
			}
			else { // 24-hour mode
				hour += ((byte)(clockData[0x02] & 0x3f)).ToDecimal();
			}

			return new DateTime(
				clockData[0x06].ToDecimal() + 2000, // year
				clockData[0x05].ToDecimal(), // month
				clockData[0x04].ToDecimal(), // day
				hour, // hour
				clockData[0x01].ToDecimal(), // minutes
				((byte)(clockData[0x00] & 0x7f)).ToDecimal());
		}

		private byte[] EncodeDate(DateTime value, bool clockHalt, bool hourMode) {
			byte seconds = 0x00;
			if (clockHalt) seconds |= 0x80;
			seconds |= (byte)(value.Second.ToBcd() & 0x7f);

			byte hour = 0x00;
			if (hourMode) { // 12-hour mode
				hour |= 0x40; //set the 12-hour flag
				if (value.Hour >= 12)
					hour |= 0x20;
				hour |= ((value.Hour % 12) + 1).ToBcd();
			}
			else { //24-hour mode
				hour |= value.Hour.ToBcd();
			}

			return new byte[DS1307_RTC_SIZE] { 
				seconds, 
				value.Minute.ToBcd(), 
				hour, 
				((int)value.DayOfWeek).ToBcd(), 
				value.Day.ToBcd(), 
				value.Month.ToBcd(), 
				(value.Year - 2000).ToBcd()};
		}

		/// <summary>
		/// Enables / Disables the square wave generation function of the clock.
		/// Requires a pull-up resistor on the clock's SQW pin.
		/// </summary>
		/// <param name="Freq">Desired frequency or disabled</param>
		/// <param name="OutCtrl">Logical level of output pin when the frequency is disabled (zero by default)</param>
		public void SetSquareWave(SQWFreq Freq, SQWDisabledOutputControl OutCtrl = SQWDisabledOutputControl.Zero) {
			byte SqwCtrlReg = (byte) OutCtrl;
			
			SqwCtrlReg <<= 3;   // bit 7 defines the square wave output level when disabled
								// bit 6 & 5 are unused

			if (Freq != SQWFreq.SQW_OFF) {
				SqwCtrlReg |= 1;
			}

			SqwCtrlReg <<= 4; // bit 4 defines if the oscillator generating the square wave frequency is on or off.
							  // bit 3 & 2 are unused
			
			SqwCtrlReg |= (byte) Freq; // bit 1 & 0 define the frequency of the square wave
			
			WriteRegister(DS1307_SQUARE_WAVE_CTRL_REGISTER_ADDRESS, SqwCtrlReg);
		}

		/// <summary>
		/// Halts / Resumes the time-keeping function on the clock.
		/// The value of the seconds register will be preserved.
		/// (True: halt, False: resume)
		/// </summary>
		public bool ClockHalt {
			get {
				var seconds = this[0x00];
				return (seconds & 0x80) == 0x80;
			}
			set {
				lock (clock) {
					var seconds = this[0x00];

					if (value)
						seconds |= 0x80; // Set bit 7
					else
						seconds &= 0x7f; // Reset bit 7

					WriteRegister(0x00, seconds);
				}
			}
		}

		/// <summary>
		/// Gets/Sets the Hour mode.
		/// The current time will be corrected. 
		/// (True: 12-hour, False: 24-hour)
		/// </summary>
		public bool TwelveHourMode {
			get {
				var hours = this[0x02];
				return (hours & 0x40) == 0x40;
			}
			set {
				lock (clock) {
					var rtcBuffer = ReadRegister(DS1307_RTC_START_ADDRESS, 7);
					var currentDate = ParseDate(rtcBuffer);
					rtcBuffer = EncodeDate(currentDate, false, value);

					WriteRegister(0x02, rtcBuffer[0x02]);
				}
			}
		}

		/// <summary>
		/// Writes to the clock's user RAM registers as a block
		/// </summary>
		/// <param name="buffer">A byte buffer of size DS1307_RAM_SIZE</param>
		public void SetRAM(byte[] buffer) {
			if (buffer.Length != DS1307_RAM_SIZE)
				throw new ArgumentOutOfRangeException("Invalid buffer length");

			WriteRegister(DS1307_RAM_START_ADDRESS, buffer);
		}

		/// <summary>
		/// Reads the clock's user RAM registers as a block.
		/// </summary>
		/// <returns>A byte array of size DS1307_RAM_SIZE containing the user RAM data</returns>
		public byte[] GetRAM() {
			return ReadRegister(DS1307_RAM_START_ADDRESS, DS1307_RAM_SIZE);
		}

		public byte this[byte address] {
			get { return ReadRegister(address, 1)[0]; }
			set { WriteRegister(address, value); }
		}

		/// <summary>
		/// Reads an arbitrary RTC or RAM register
		/// </summary>
		/// <param name="address">Register address between 0x00 and 0x3f</param>
		/// <param name="length">The number of bytes to read</param>
		/// <returns>The value of the bytes read at the address</returns>
		public byte[] ReadRegister(byte address, int length = 1) {
			if (length < 1) throw new ArgumentOutOfRangeException("length", "Must read at least 1 byte");
			if (address + length -1 > DS1307_RAM_END_ADDRESS) throw new ArgumentOutOfRangeException("Invalid register address");

			var buffer = new byte[length];

			lock (clock) {
				clock.Config = config;
				// Read the RAM register @ the address
				var transaction = new I2CDevice.I2CTransaction[] {
						I2CDevice.CreateWriteTransaction(new byte[] {address}),
						I2CDevice.CreateReadTransaction(buffer) 
					};

				if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
					throw new Exception("I2C transaction failed");
				}
			}

			return buffer;
		}

		/// <summary>
		/// Writes an arbitrary RTC or RAM register
		/// </summary>
		/// <param name="address">Register address between 0x00 and 0x3f</param>
		/// <param name="val">The value of the byte to write at that address</param>
		public void WriteRegister(byte address, byte data) {
			WriteRegister(address, new byte[] { data });
		}

		public void WriteRegister(byte address, byte[] data) {
			if (address > DS1307_RAM_END_ADDRESS)
				throw new ArgumentOutOfRangeException("Invalid register address");
			if (address + data.Length > DS1307_RAM_END_ADDRESS)
				throw new ArgumentException("Buffer overrun");

			byte[] txData = new byte[data.Length + 1];
			txData[0] = address;
			data.CopyTo(txData, 1);

			lock (clock) {
				clock.Config = config;
				var transaction = new I2CDevice.I2CWriteTransaction[] {
					I2CDevice.CreateWriteTransaction(txData)
				};

				if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
					throw new Exception("I2C write transaction failed");
				}
			}
		}

		

		
		
		
		/// <summary>
		/// Releases clock resources
		/// </summary>
		public void Dispose() {
			if(privateClock)
				clock.Dispose();
		}
	}
}

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

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

        // Square wave frequency generator register address
        public const byte DS1307_SQUARE_WAVE_CTRL_REGISTER_ADDRESS = 0x07;

        // Start / End addresses of the user RAM registers
        public const byte DS1307_RAM_START_ADDRESS = 0x08;
        public const byte DS1307_RAM_END_ADDRESS = 0x3f;

        // Total size of the user RAM block
        public const byte DS1307_RAM_SIZE = 56;

        // Instance of the I2C clock
        I2CDevice clock;

        public DS1307() {
            clock = new I2CDevice(new I2CDevice.Configuration(DS1307_I2C_ADDRESS, DS1307_I2C_CLOCK_RATE_KHZ));
        }

        /// <summary>
        /// Gets the date / time in 24 hour format.
        /// </summary>
        /// <returns>A DateTime object</returns>
        public DateTime Get() {
            byte[] clockData = new byte [7];

            // Read time registers (7 bytes from DS1307_RTC_START_ADDRESS)
            var transaction = new I2CDevice.I2CTransaction[] {
                I2CDevice.CreateWriteTransaction(new byte[] {DS1307_RTC_START_ADDRESS}),
                I2CDevice.CreateReadTransaction(clockData)
            };

            if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                throw new Exception("I2C transaction failed");
            }

            return new DateTime(
                BcdToDec(clockData[6]) + 2000, // year
                BcdToDec(clockData[5]), // month
                BcdToDec(clockData[4]), // day
                BcdToDec(clockData[2] & 0x3f), // hours over 24 hours
                BcdToDec(clockData[1]), // minutes
                BcdToDec(clockData[0] & 0x7f) // seconds
                );
        }
      
        /// <summary>
        /// Sets the time on the clock using the datetime object. Milliseconds are not used.
        /// </summary>
        /// <param name="dt">A DateTime object used to set the clock</param>
        public void Set(DateTime dt) {
            var transaction = new I2CDevice.I2CWriteTransaction[] {
                I2CDevice.CreateWriteTransaction(new byte[] { 
                                  DS1307_RTC_START_ADDRESS, 
                                  DecToBcd(dt.Second), 
                                  DecToBcd(dt.Minute), 
                                  DecToBcd(dt.Hour), 
                                  DecToBcd((int)dt.DayOfWeek), 
                                  DecToBcd(dt.Day), 
                                  DecToBcd(dt.Month), 
                                  DecToBcd(dt.Year - 2000)} )
            };

            if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                throw new Exception("I2C write transaction failed");
            }
        }

        /// <summary>
        /// Enables / Disables the square wave generation function of the clock.
        /// Requires a pull-up resistor on the clock's SQW pin.
        /// </summary>
        /// <param name="Freq">Desired frequency or disabled</param>
        /// <param name="OutCtrl">Logical level of output pin when the frequency is disabled (zero by default)</param>
        public void SetSquareWave(SQWFreq Freq, SQWDisabledOutputControl OutCtrl = SQWDisabledOutputControl.Zero)
        {
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
        /// Calling this function preserves the value of the seconds register.
        /// </summary>
        /// <param name="halt">True: halt, False: resume</param>
        public void Halt(bool halt) {
            var seconds = this[DS1307_RTC_START_ADDRESS];

            if (halt) {
                seconds |= 0x80; // Set bit 7
            }
            else {
                seconds &= 0x7f; // Reset bit 7
            }

            WriteRegister(DS1307_RTC_START_ADDRESS, seconds);
        }

        /// <summary>
        /// Writes to the clock's user RAM registers as a block
        /// </summary>
        /// <param name="buffer">A byte buffer of size DS1307_RAM_SIZE</param>
        public void SetRAM(byte[] buffer) {
            if (buffer.Length != DS1307_RAM_SIZE) {
                throw new ArgumentOutOfRangeException("Invalid buffer length");
            }

            // Allocate a new buffer large enough to include the RAM start address byte and the payload
            var TrxBuffer = new byte[sizeof(byte) /*Address byte*/ + DS1307_RAM_SIZE];

            // Set the RAM start address
            TrxBuffer[0] = DS1307_RAM_START_ADDRESS;

            // Copy the user buffer after the address
            buffer.CopyTo(TrxBuffer, 1);

            // Write to the clock's RAM
            var transaction = new I2CDevice.I2CWriteTransaction[] {I2CDevice.CreateWriteTransaction(TrxBuffer)};

            if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                throw new Exception("I2C write transaction failed");
            }
        }

        /// <summary>
        /// Reads the clock's user RAM registers as a block.
        /// </summary>
        /// <returns>A byte array of size DS1307_RAM_SIZE containing the user RAM data</returns>
        public byte[] GetRAM() {
            var ram = new byte[DS1307_RAM_SIZE];

            var transaction = new I2CDevice.I2CTransaction[] {
                    I2CDevice.CreateWriteTransaction(new byte[] {DS1307_RAM_START_ADDRESS}),
                    I2CDevice.CreateReadTransaction(ram) 
                    };

            if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                throw new Exception("I2C transaction failed");
            }

            return ram;
        }

        /// <summary>
        /// Reads an arbitrary RTC or RAM register
        /// </summary>
        /// <param name="address">Register address between 0x00 and 0x3f</param>
        /// <returns>The value of the byte read at the address</returns>
        public byte this[byte address] {
            get {
                if (address > DS1307_RAM_END_ADDRESS) {
                    throw new ArgumentOutOfRangeException("Invalid register address");
                }

                var value = new byte[1];

                // Read the RAM register @ the address
                var transaction = new I2CDevice.I2CTransaction[] {
                    I2CDevice.CreateWriteTransaction(new byte[] {address}),
                    I2CDevice.CreateReadTransaction(value) 
                    };

                if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                    throw new Exception("I2C transaction failed");
                }

                return value[0];
            }
        }

        /// <summary>
        /// Writes an arbitrary RTC or RAM register
        /// </summary>
        /// <param name="address">Register address between 0x00 and 0x3f</param>
        /// <param name="val">The value of the byte to write at that address</param>
        public void WriteRegister(byte address, byte val) {
            if (address > DS1307_RAM_END_ADDRESS) {
                throw new ArgumentOutOfRangeException("Invalid register address");
            }

            var transaction = new I2CDevice.I2CWriteTransaction[] {
                I2CDevice.CreateWriteTransaction(new byte[] {address, (byte) val})
            };

            if (clock.Execute(transaction, DS1307_I2C_TRANSACTION_TIMEOUT_MS) == 0) {
                throw new Exception("I2C write transaction failed");
            }
        }

        /// <summary>
        /// Takes a Binary-Coded-Decimal value and returns it as an integer value
        /// </summary>
        /// <param name="val">BCD encoded value</param>
        /// <returns>An integer value</returns>
        protected int BcdToDec(int val) {
            return ((val / 16 * 10) + (val % 16));
        }

        /// <summary>
        /// Takes a Decimal value and converts it into a Binary-Coded-Decimal value
        /// </summary>
        /// <param name="val">Value to be converted</param>
        /// <returns>A BCD-encoded value</returns>
        protected byte DecToBcd(int val) {
            return (byte)((val / 10 * 16) + (val % 10));
        }
        
        /// <summary>
        /// Releases clock resources
        /// </summary>
        public void Dispose() {
            clock.Dispose();
        }
    }
}

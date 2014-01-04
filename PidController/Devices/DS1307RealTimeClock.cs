//Via http://forums.netduino.com/index.php?/topic/215-ds1307-real-time-clock/
using System;
using Microsoft.SPOT.Hardware;

namespace PidController.Devices
{
    /// <summary>
    /// This class interfaces with the DS1307 real time clock chip via the I2C bus.
    /// To wire the chip to a netduino board, wire as follows:
    /// SDA -> Analog Pin 4
    /// SCL -> Analog Pin 5
    /// GND -> GND
    /// 5V  -> 5V
    /// </summary>
    public class DS1307RealTimeClock : IDisposable
    {
        private const int I2CAddress = 0x68;
        private const int I2CTimeout = 1000;
        private const int I2CClockRateKhz = 100;

        public const int UserDataAddress = 8;
        public const int UserDataLength = 56;

        private I2CDevice clock = new I2CDevice(new I2CDevice.Configuration(I2CAddress, I2CClockRateKhz));

        /// <summary>
        /// Set the local .NET time from the RTC board. You can do this on startup then call
        /// DateTime.Now during program execution.
        /// </summary>
        public void SetLocalTimeFromRTC()
        {
            var dt = Now();
            Utility.SetLocalTime(dt);
        }


        /// <summary>
        /// This method sets the real time clock. The current implementation does not take into account control
        /// registers on the DS1307. They can be easily added if needed.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="dayofWeek"></param>
        /// <returns></returns>
        public int SetClock(byte year, byte month, byte day, byte hour, byte minute, byte second, DayOfWeek dayofWeek)
        {
            // Set the time
            var buffer = new byte[] 
            {
                0x00, // Address
                (byte)second.ToBCD(),
                (byte)minute.ToBCD(),
                (byte)hour.ToBCD(),
                ((byte)(dayofWeek +1)).ToBCD(),
                (byte)day.ToBCD(),
                (byte)month.ToBCD(),
                (byte)year.ToBCD()
            };

            var transaction = new I2CDevice.I2CTransaction[] 
            {
                I2CDevice.CreateWriteTransaction(buffer)
            };

            return clock.Execute(transaction, I2CTimeout);
        }

        /// <summary>
        /// Reads data from the DS1307 clock registers and returns it as a .NET DateTime.
        /// </summary>
        /// <returns></returns>
        public DateTime Now()
        {
            var data = new byte[7];
            int result = Read(0, data);

            //TODO: Add exception handling if result == 0

            var dt = new DateTime(
                2000 + data[6].FromBCD(),               // Year
                data[5].FromBCD(),                      // Month
                data[4].FromBCD(),                      // Day
                ((byte)(data[2] & 0x3f)).FromBCD(),     // Hour
                data[1].FromBCD(),                      // Minute
                ((byte)(data[0] & 0x7f)).FromBCD()      // Second
                );

            return dt;
        }


        /// <summary>
        /// Write data to the clock memory. Normally, this will be used for writing to the user data area.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Write(byte address, byte[] data)
        {
            byte[] buffer = new byte[57];
            buffer[0] = address;
            data.CopyTo(buffer, 1);

            var transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(buffer)
            };

            return clock.Execute(transaction, I2CTimeout);
        }

        /// <summary>
        /// Read data from the clock memory. Normally this will be used for reading data from the user memory area.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Read(byte address, byte[] data)
        {
            var transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(new byte[] {address}),     // Go to address
                I2CDevice.CreateReadTransaction(data)                       // Read the clock registers
            };

            return clock.Execute(transaction, I2CTimeout);
        }


        #region IDisposable Members
        // The skeleton for this implementaion of IDisposable is taken directly from MSDN.
        // I have left the MSDN comments in place for reference.

        // Track whether Dispose has been called.
        private bool disposed = false;

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (clock != null)
                        clock.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                /* Empty */

                // Note disposing has been done.
                disposed = true;

            }
        }

        #endregion
    }

}

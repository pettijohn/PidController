using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using PidController.Devices;

namespace PidController
{
    public class Program
    {
        public static void Main()
        {
            //Address should be either 0x20 or 0x27, depending on if pins 6,7,9 (A2,A1,A0) are high or low.
            byte address = 0x27;
            Devices.LiquidCrystal_I2C display;

            //Both seem to work
            //display = new Devices.LiquidCrystal_I2C(address, 20, 4);
            display = new Devices.LiquidCrystal_I2C(address, 40, 2);
            Debug.Print("Success 0x" + address.ToString("X"));
            display.setBacklight(true);
            display.write(0x1D);
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");
            display.write("Hello world");

            /* I2C Real Time Clock
            using (var clock = new DS1307RealTimeClock())
            {
                // TODO: Do this only once to set your clock
                //clock.SetClock(13, 09, 26, 19, 23, 0, DayOfWeek.Tuesday);

                clock.SetLocalTimeFromRTC();

                Debug.Print("System has started at: " + DateTime.Now.ToString());
            }

            InputPort button = new InputPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled);
            bool buttonState = false;

            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
            while (true)
            {
                led.Write(false);
                Thread.Sleep(100); 
                buttonState = button.Read();
                if (buttonState)
                {
                    RederTime(led);
                }
            }*/


        }

        /// <summary>
        /// Blink the time: hours, pause, ten minutes, pause, one minutes.
        /// </summary>
        /// <param name="led"></param>
        private static void RederTime(OutputPort led)
        {
            var now = DateTime.Now;
            var hour = now.Hour % 12;
            var tenMinute = now.Minute / 10;
            var rMinute = now.Minute % 10;
            var segments = new[] {hour, tenMinute, rMinute};

            foreach (var segment in segments)
            {
                if(segment == 0)
                {
                    led.Write(true);
                    Thread.Sleep(1500);
                    led.Write(false);
                }
                for (int i = 0; i < segment; i++)
                {
                    led.Write(true);
                    Thread.Sleep(500);
                    led.Write(false);
                    Thread.Sleep(250);
                }
                Thread.Sleep(1000);
            }
        }


    }
}

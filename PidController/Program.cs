using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using PidController.Devices;
using Toolbox.NETMF.Hardware;

namespace PidController
{
    public class Program
    {

        static int RotaryValue = 0;

        public static void Main()
        {
            // Initializes a new rotary encoder object
            RotaryEncoder Knob = new RotaryEncoder(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1);
            // Bounds the event to the rotary encoder
            Knob.Rotated += new NativeEventHandler(Knob_Rotated);

         
            var clock = new Toolbox.NETMF.Hardware.DS1307(0x68, 100);
            //clock.SetTime(2014, 01, 05, 11, 13, 0);
            clock.Synchronize();
            Debug.Print("System has started at: " + DateTime.Now.ToString());


            //Both 40x2 and 20x4 seem to work
            var display = new Devices.LiquidCrystal_I2C(0x27, 40, 2);
            display.setBacklight(true);

            while (true)
            {
                display.setCursor(0,0);
                display.write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + RotaryValue);
                Thread.Sleep(100);
            }
          
          


            //This renders as an arrow ->
            //display.write((byte)0x7E);
            //display.write("~");

            //This renders as ° Degree
            //display.write("ß");
            //display.write((byte)0xDF);

            //Square Cursor
            //display.write((byte)0xFF);
            //display.write("ÿ");
            


            /* I2C Real Time Clock
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
        /// The value has been changed
        /// </summary>
        /// <param name="Unused">Not used</param>
        /// <param name="Value">The new value</param>
        /// <param name="Time">Time of the event</param>
        static void Knob_Rotated(uint Unused, uint Value, DateTime Time)
        {
            Debug.Print(DateTime.Now.Ticks.ToString());
            Debug.Print(Value == 1 ? "Clockwise" : "Counter clockwise");
            RotaryValue += Value == 1 ? 1 : -1;
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

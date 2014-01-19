using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace PidController.Devices
{
    /// <summary>
    /// Measure distance with a HC-SR04 ultrasonic module
    /// </summary>
    public class UltrasonicDistance
    {
        public UltrasonicDistance(Cpu.Pin trigger, Cpu.Pin echo)
        {
            Trigger = new OutputPort(trigger, false);
            
            Echo = new InterruptPort(echo, false, Port.ResistorMode.PullDown, Port.InterruptMode.InterruptEdgeBoth);
            Echo.OnInterrupt += Echo_OnInterrupt;
        }

        private long _lastHigh;

        void Echo_OnInterrupt(uint pinId, uint value, DateTime time)
        {
            if (value > 0)
            {
                _lastHigh = time.Ticks;
                return;
            }
            //A single tick represents one hundred nanoseconds or one ten-millionth of a second. There are 10,000 ticks in a millisecond.
            double delta = time.Ticks - _lastHigh;

            //Debug.Print("High for ticks: " + delta.ToString());
            //Debug.Print("High for ms   : " + (delta / 10000).ToString());

            // 1 tick == 100 ns == 0.1 µs ==> 10 ticks == 1 µs
            var widthMicroSec = delta / 10.0;
            // Per data sheet, compute distance:
            // pulse width (uS) / 58 = distance (cm)
            DistanceCM = widthMicroSec / 58;
            OnDistanceChanged(DistanceCM);

            //Debug.Print("Distance in cm: " + cm.ToString("F1"));
        }

        public OutputPort Trigger { get; private set; }

        public InterruptPort Echo { get; private set; }

        /// <summary>
        /// Distance in centimeters. Use ToString("F1") to format to 1 decimal place. Spec says accurate to 0.3cm
        /// </summary>
        public double DistanceCM { get; set; }

        /// <summary>
        /// Fired when a new distance reading has completed
        /// </summary>
        public event DistanceChangedEventHandler DistanceChanged;

        /// <summary>
        /// Triggers <see cref="DistanceChanged"/>
        /// </summary>
        /// <param name="distanceCM"></param>
        protected void OnDistanceChanged(double distanceCM)
        {
            if (DistanceChanged != null)
                DistanceChanged(new DistanceChangedEventArgs(distanceCM));
        }

        /// <summary>
        /// Updates <see cref="DistanceCM"/>, which in turn fires <see cref="DistanceChanged"/>
        /// </summary>
        public void ReadDistance()
        {
            //Pulse high for 10µs
            Trigger.Write(true);
            Thread.Sleep(1);
            Trigger.Write(false);

        }
    }

    public delegate void DistanceChangedEventHandler(DistanceChangedEventArgs args);

    public class DistanceChangedEventArgs : EventArgs
    {
        public DistanceChangedEventArgs(double distanceCM)
        {
            DistanceCM = distanceCM;
        }

        public double DistanceCM { get; private set; }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace PidController
{
    public class Program
    {
        public static void Main()
        {
            var diFramework = new DIFramework();
            diFramework.Register<ISensorState>().To<SensorState>().AsSingleton();
            diFramework.Register<IThermocouple>().To<Thermocouple>().AsSingleton();

            var scheduler = new TaskSchedulerEngine();
            scheduler.ResolveReference = (t) =>
            {
                return diFramework.Resolve(t);
            };

            scheduler.Add(new Schedule("everySecond").EverySecond().Execute<IThermocouple>);

            scheduler.Start();
            scheduler.Wait();
        }

    }
}

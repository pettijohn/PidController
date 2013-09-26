using System;
using Microsoft.SPOT;
using MFUnit;

namespace PidController.Tests
{
    public class Program : TestApplication
    {
        public static void Main() 
        {
            new Program().Run();
            Debug.Print("Hello world");
        }

    }
}

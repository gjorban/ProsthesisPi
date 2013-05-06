using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

using System.Xml;

using ArduinoCommunicationsLibrary;

namespace Arduino_Communications_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = string.Format("Arduino-comms-{0}.txt", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            ProsthesisCore.Utility.Logger logger = new ProsthesisCore.Utility.Logger(fileName, true);

            ArduinoCommsBase.InitializeSerialConnections(logger);
            ArduinoCommsBase test = new MotorControllerArduino(logger);
            bool telemEnable = false;
            bool arduinoState = false;
            if (test.StartArduinoComms())
            {
                Console.WriteLine("Press 'x' to exit. 'T' to enable telemetry, 'E' to toggle device state");
                ConsoleKey key = ConsoleKey.A;
                do
                {
                    System.Threading.Thread.Sleep(16);
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey().Key;

                        if (key == ConsoleKey.T)
                        {
                            telemEnable = !telemEnable;
                            test.TelemetryToggle(500);
                        }
                        else if (key == ConsoleKey.E)
                        {
                            arduinoState = !arduinoState;
                            test.ToggleArduinoState(arduinoState);
                        }
                    }
                }
                while (key != ConsoleKey.X);

                test.StopArduinoComms(true);
            }
            else
            {
                Console.WriteLine("Failed to connect to the correct Arduino. Press any key to exit");
                do
                {
                    System.Threading.Thread.Sleep(16);
                }
                while (!Console.KeyAvailable);
            }

            logger.ShutDown();
        }
    }
}

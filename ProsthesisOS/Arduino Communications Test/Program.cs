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

            System.DateTime start = System.DateTime.Now;

            string telemFileName = string.Format("Arduino-telem-{0}.csv", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            System.IO.TextWriter writer = new System.IO.StreamWriter(telemFileName);

            ArduinoCommsBase.InitializeSerialConnections(logger);
            MotorControllerArduino test = new MotorControllerArduino(logger);

            test.TelemetryUpdate += new Action<ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry>(delegate(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry obj) {
                double ts = (System.DateTime.Now - start).Duration().TotalMilliseconds;

                string telemRow = string.Format("{0},{1},{2},{3},{4},{5}", ts, obj.MotorDutyCycles[1], obj.OutputPressure[1], obj.ProportionalTunings[1], obj.IntegralTunings[1], obj.DifferentialTunings[1]);
                Console.WriteLine(telemRow);
                writer.WriteLine(telemRow);
            });

            bool telemEnable = false;
            bool arduinoState = false;
            if (test.StartArduinoComms())
            {
                Console.WriteLine("Press 'x' to exit. 'T' to enable telemetry, 'E' to toggle device state");
                ConsoleKey key = ConsoleKey.A;

                test.ToggleHeartbeat(true, 1000, 3);
                do
                {
                    System.Threading.Thread.Sleep(16);
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey().Key;

                        if (key == ConsoleKey.T)
                        {
                            telemEnable = !telemEnable;
                            test.TelemetryToggle(telemEnable ? 25 : 0);
                        }
                        else if (key == ConsoleKey.E)
                        {
                            arduinoState = !arduinoState;
                            test.ToggleArduinoState(arduinoState);
                        }
                    }
                }
                while (key != ConsoleKey.X && test.IsConnected);

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

            writer.Close();
            logger.ShutDown();
        }
    }
}

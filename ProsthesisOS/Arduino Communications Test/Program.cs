using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Timers;

using System.Net;
using System.Net.Sockets;

using System.Xml;

using ArduinoCommunicationsLibrary;
using ProsthesisCore;
using ProsthesisCore.Telemetry;
using ProsthesisCore.Messages;

namespace Arduino_Communications_Test
{
    class Program
    {
        private const double kTelemetryBroadcastPeriod = 16;
        private static ProsthesisCore.Telemetry.ProsthesisTelemetry mTelem = new ProsthesisCore.Telemetry.ProsthesisTelemetry();
        private static Timer mTelemetryBroadcastTimer = null;

        static void Main(string[] args)
        {
            mTelemetryBroadcastTimer = new Timer(kTelemetryBroadcastPeriod);
            mTelemetryBroadcastTimer.AutoReset = true;
            mTelemetryBroadcastTimer.Elapsed += OnTelemetryPublishAlarm;
            mTelemetryBroadcastTimer.Start();

            string fileName = string.Format("Arduino-comms-{0}.txt", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            ProsthesisCore.Utility.Logger logger = new ProsthesisCore.Utility.Logger(fileName, true);

            System.DateTime start = System.DateTime.Now;

            string telemFileName = string.Format("Arduino-telem-{0}.csv", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            System.IO.TextWriter writer = new System.IO.StreamWriter(telemFileName);

            ArduinoCommsBase.InitializeSerialConnections(logger);
            MotorControllerArduino test = new MotorControllerArduino(logger);

            writer.WriteLine("Timestamp(ms), Duty Cycle 1, Duty Cycle 2, Pressure 1, pressure 2, P, I, D");
            test.TelemetryUpdate += new Action<ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry>(delegate(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry obj) {
                double ts = (System.DateTime.Now - start).Duration().TotalMilliseconds;
                mTelem.MotorTelem = obj;
                string telemRow = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", ts, obj.MotorDutyCycles[1],obj.MotorDutyCycles[0], obj.OutputPressure[1], obj.OutputPressure[0], obj.ProportionalTunings[1], obj.IntegralTunings[1], obj.DifferentialTunings[1], obj.PressureSetPoints[0], obj.PressureSetPoints[1]);
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

            mTelemetryBroadcastTimer.Start();
            writer.Close();
            logger.ShutDown();
        }

        private static void OnTelemetryPublishAlarm(object sender, ElapsedEventArgs arg)
        {
            using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
            {
                ProsthesisCore.Telemetry.ProsthesisTelemetry telem = mTelem;
                ProsthesisDataPacket packet = ProsthesisCore.Messages.ProsthesisDataPacket.BoxMessage<ProsthesisCore.Telemetry.ProsthesisTelemetry>(telem);

                ProsthesisCore.Telemetry.ProsthesisTelemetry newTelem = ProsthesisCore.Messages.ProsthesisDataPacket.UnboxMessage(packet) as ProsthesisCore.Telemetry.ProsthesisTelemetry;


                if (packet != null)
                {
                    IPAddress address = IPAddress.Parse(ProsthesisCore.ProsthesisConstants.kMulticastGroupAddress);
                    IPEndPoint ipEndPoint = new IPEndPoint(address, ProsthesisCore.ProsthesisConstants.kTelemetryPort);
                    udpClient.JoinMulticastGroup(address, 50);
                    udpClient.Send(packet.Bytes, packet.Bytes.Length, ipEndPoint);
                    udpClient.Close();
                }
                else
                {
                    // mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Telemetry, "Failed to box telemetry message");
                }
            }
        }
    }
}

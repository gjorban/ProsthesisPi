using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Text;

using ProsthesisOS.TCP;
using ProsthesisCore.Messages;

namespace ProsthesisOS
{
    public static class Program
    {
        private static ProsthesisCore.Utility.Logger mLogger = null;
        public static ProsthesisCore.Utility.Logger Logger { get { return mLogger; } }

        private static Timer mTelemetryBroadcastTimer = null;
        private static States.ProsthesisMainContext mContext = null;

        private const double kTelemetryBroadcastPeriod = 16;

        public static void Main(string[] args)
        {
            mTelemetryBroadcastTimer = new Timer(kTelemetryBroadcastPeriod);
            mTelemetryBroadcastTimer.AutoReset = true;
            mTelemetryBroadcastTimer.Elapsed += OnTelemetryPublishAlarm;

            string fileName = string.Format("Server-{0}.txt", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            mLogger = new ProsthesisCore.Utility.Logger(fileName, true);
            mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.General, "ProsthesisOS startup", true);

            mContext = new States.ProsthesisMainContext(ProsthesisCore.ProsthesisConstants.ConnectionPort, mLogger);

            mTelemetryBroadcastTimer.Start();

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                if (mContext.IsRunning)
                {
                    mContext.Terminate("Aborted");
                }

                mLogger.ShutDown();
            };

            Console.WriteLine("Press 'x' to exit");

            while (Console.ReadKey().Key != ConsoleKey.X) { }

            mTelemetryBroadcastTimer.Stop();
            mLogger.ShutDown();
        }

        private static void OnTelemetryPublishAlarm(object sender, ElapsedEventArgs arg)
        {
            using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
            {
                ProsthesisCore.Telemetry.ProsthesisTelemetry telem = mContext.MachineState;
                ProsthesisDataPacket packet = ProsthesisCore.Messages.ProsthesisDataPacket.BoxMessage<ProsthesisCore.Telemetry.ProsthesisTelemetry>(telem);

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
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Telemetry, "Failed to box telemetry message");
                }
            }
        }
    }
}

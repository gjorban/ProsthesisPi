using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Timers;

using Timer = System.Timers.Timer;

using ProsthesisCore;
using ProsthesisCore.Messages;

namespace ProsthesisTelemetrySimulator
{
    public partial class TelemetrySimulator : Form
    {
        private Timer mTelemetryBroadcastTimer = null;

        private const double kTelemetryBroadcastPeriod = 16;
        private ProsthesisCore.Telemetry.ProsthesisTelemetry mTelem = new ProsthesisCore.Telemetry.ProsthesisTelemetry();

        public TelemetrySimulator()
        {
            mTelemetryBroadcastTimer = new Timer(kTelemetryBroadcastPeriod);
            mTelemetryBroadcastTimer.AutoReset = true;
            mTelemetryBroadcastTimer.Elapsed += OnTelemetryPublishAlarm;

            InitializeComponent();

            int numMotorsToSimulate = 2;
            ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry motorTelem = mTelem.MotorTelem;
            motorTelem.Current = new float[numMotorsToSimulate];
            motorTelem.MilliVolts = new int[numMotorsToSimulate];
            motorTelem.FlowRate = new float[numMotorsToSimulate];
            motorTelem.MotorDutyCycles = new float[numMotorsToSimulate];
            motorTelem.PressureLoad = new float[numMotorsToSimulate];
            motorTelem.OutputPressure = new float[numMotorsToSimulate];
            motorTelem.PressureSetPoints = new float[numMotorsToSimulate];

            ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisSensorTelemetry sensTelem = mTelem.SensorTelem;
            sensTelem.MotorTemps = new float[numMotorsToSimulate];
            sensTelem.OilTemps = new float[1];

            mTelemetryGrid.SelectedObject = mTelem;
        }

        private void OnTelemetryPublishAlarm(object sender, ElapsedEventArgs arg)
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

        private void mTelemStart_Click(object sender, EventArgs e)
        {
            mTelemetryBroadcastTimer.Start();
            mTelemStart.Enabled = false;
            mTelemStop.Enabled = true;
        }

        private void mTelemStop_Click(object sender, EventArgs e)
        {
            mTelemetryBroadcastTimer.Stop();
            mTelemStart.Enabled = true;
            mTelemStop.Enabled = false;
        }
    }
}

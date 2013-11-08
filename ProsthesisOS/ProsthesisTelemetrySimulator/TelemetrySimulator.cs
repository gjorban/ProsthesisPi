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
using ProsthesisCore.Telemetry;
using ProsthesisCore.Messages;

namespace ProsthesisTelemetrySimulator
{
    public partial class TelemetrySimulator : Form
    {
        private bool mMirroringValues
        {
            get { return mMirrorMotorValues.Checked; }
        }

        private Timer mTelemetryBroadcastTimer = null;

        private const double kTelemetryBroadcastPeriod = 16;
        private ProsthesisCore.Telemetry.ProsthesisTelemetry mTelem = new ProsthesisCore.Telemetry.ProsthesisTelemetry();

        public TelemetrySimulator()
        {
            mTelemetryBroadcastTimer = new Timer(kTelemetryBroadcastPeriod);
            mTelemetryBroadcastTimer.AutoReset = true;
            mTelemetryBroadcastTimer.Elapsed += OnTelemetryPublishAlarm;

            InitializeComponent();

            //Initialize telemetry data
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

            //Set all GUI limits
            mHipThrottleSlider.Maximum = 100;
            mHipThrottleNumeric.Maximum = 100;
            mKneeThrottleSlider.Maximum = 100;
            mKneeThrottleNumeric.Maximum = 100;

            mHipSetpointSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mHipSetpointNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mKneeSetpointSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mKneeSetpointNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;

            mHipOutputPressureSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mHipOutputPressureNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mKneeOutputPressureSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;
            mKneeOutputPressureNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxPressurePSI;

            mHipFlowrateSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxFlowRateLPM;
            mHipFlowrateNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxFlowRateLPM;
            mKneeFlowrateSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxFlowRateLPM;
            mKneeFlowrateNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxFlowRateLPM;

            mHipMotorCurrentSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxCurrentAmps;
            mHipMotorCurrentNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxCurrentAmps;
            mKneeMotorCurrentSlider.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxCurrentAmps;
            mKneeMotorCurrentNumeric.Maximum = (int)ProsthesisTelemetry.ProsthesisMotorTelemetry.kMaxCurrentAmps;
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

        private void mMirrorMotorValues_CheckedChanged(object sender, EventArgs e)
        {
            bool kneeEnabled = !mMirroringValues;
            mKneeFlowrateSlider.Enabled = kneeEnabled;
            mKneeFlowrateSlider.Value = mHipFlowrateSlider.Value;

            mKneeFlowrateNumeric.Enabled = kneeEnabled;
            mKneeFlowrateNumeric.Value = mHipFlowrateNumeric.Value;

            mKneeMotorCurrentSlider.Enabled = kneeEnabled;
            mKneeMotorCurrentSlider.Value = mHipMotorCurrentSlider.Value;

            mKneeMotorCurrentNumeric.Enabled = kneeEnabled;
            mKneeMotorCurrentNumeric.Value = mHipMotorCurrentNumeric.Value;

            mKneeOutputPressureSlider.Enabled = kneeEnabled;
            mKneeOutputPressureSlider.Value = mHipOutputPressureSlider.Value;

            mKneeOutputPressureNumeric.Enabled = kneeEnabled;
            mKneeOutputPressureNumeric.Value = mHipOutputPressureNumeric.Value;

            mKneeSetpointSlider.Enabled = kneeEnabled;
            mKneeSetpointSlider.Value = mHipSetpointSlider.Value;

            mKneeSetpointNumeric.Enabled = kneeEnabled;
            mKneeSetpointNumeric.Value = mHipSetpointNumeric.Value;

            mKneeThrottleSlider.Enabled = kneeEnabled;
            mKneeThrottleSlider.Value = mHipThrottleSlider.Value;

            mKneeThrottleNumeric.Enabled = kneeEnabled;
            mKneeThrottleNumeric.Value = mHipThrottleNumeric.Value;
        }

        private void mHipThrottleSlider_Scroll(object sender, EventArgs e)
        {
            int value = mHipThrottleSlider.Value;
            mTelem.MotorTelem.MotorDutyCycles[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipSliderChanged(value, mHipThrottleSlider, mHipThrottleNumeric, mKneeThrottleSlider, mKneeThrottleNumeric);
        }

        private void mHipThrottleNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mHipThrottleNumeric.Value;
            mTelem.MotorTelem.MotorDutyCycles[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipNumericChanged(value, mHipThrottleSlider, mHipThrottleNumeric, mKneeThrottleSlider, mKneeThrottleNumeric);
        }

        private void mKneeThrottleSlider_Scroll(object sender, EventArgs e)
        {
            int value = mKneeThrottleSlider.Value;
            mTelem.MotorTelem.MotorDutyCycles[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeSliderChanged(value, mKneeThrottleSlider, mKneeThrottleNumeric);
        }

        private void mKneeThrottleNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mKneeThrottleNumeric.Value;
            mTelem.MotorTelem.MotorDutyCycles[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeNumericChanged(value, mKneeThrottleSlider, mKneeThrottleNumeric);
        }

        private void mHipSetpointSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mHipSetpointSlider.Value;
            mTelem.MotorTelem.Ps[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipSliderChanged(value, mHipSetpointSlider, mHipSetpointNumeric, mKneeSetpointSlider, mKneeSetpointNumeric);
        }

        private void mHipSetpointNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mHipSetpointNumeric.Value;
            mTelem.MotorTelem.Ps[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipNumericChanged(value, mHipSetpointSlider, mHipSetpointNumeric, mKneeSetpointSlider, mKneeSetpointNumeric);
        }

        private void mKneeSetpointSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mKneeSetpointSlider.Value;
            mTelem.MotorTelem.PressureSetPoints[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeSliderChanged(value, mKneeSetpointSlider, mKneeSetpointNumeric);
        }

        private void mKneeSetpointNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mKneeSetpointNumeric.Value;
            mTelem.MotorTelem.PressureSetPoints[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeNumericChanged(value, mKneeSetpointSlider, mKneeSetpointNumeric);
        }

        private void mHipOutputPressureSlider_Scroll(object sender, EventArgs e)
        {
            int value = mHipOutputPressureSlider.Value;
            mTelem.MotorTelem.Pout[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipSliderChanged(value, mHipOutputPressureSlider, mHipOutputPressureNumeric, mKneeOutputPressureSlider, mKneeOutputPressureNumeric);
        }

        private void mHipOutputPressureNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mHipOutputPressureNumeric.Value;
            mTelem.MotorTelem.Pout[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipNumericChanged(value, mHipOutputPressureSlider, mHipOutputPressureNumeric, mKneeOutputPressureSlider, mKneeOutputPressureNumeric);
        }

        private void mKneeOutputPressureSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mKneeOutputPressureSlider.Value;
            mTelem.MotorTelem.Pout[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeSliderChanged(value, mKneeOutputPressureSlider, mKneeOutputPressureNumeric);
        }

        private void mKneeOutputPressureNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mKneeOutputPressureNumeric.Value;
            mTelem.MotorTelem.PressureSetPoints[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeNumericChanged(value, mKneeOutputPressureSlider, mKneeOutputPressureNumeric);
        }

        private void mHipFlowrateSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mHipFlowrateSlider.Value;
            mTelem.MotorTelem.FlowRate[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipSliderChanged(value, mHipFlowrateSlider, mHipFlowrateNumeric, mKneeFlowrateSlider, mKneeFlowrateNumeric);
        }

        private void mHipFlowrateNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mHipFlowrateNumeric.Value;
            mTelem.MotorTelem.FlowRate[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipNumericChanged(value, mHipFlowrateSlider, mHipFlowrateNumeric, mKneeFlowrateSlider, mKneeFlowrateNumeric);
        }

        private void mKneeFlowrateSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mKneeFlowrateSlider.Value;
            mTelem.MotorTelem.FlowRate[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeSliderChanged(value, mKneeFlowrateSlider, mKneeFlowrateNumeric);
        }

        private void mKneeFlowrateNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mKneeFlowrateNumeric.Value;
            mTelem.MotorTelem.FlowRate[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeNumericChanged(value, mKneeFlowrateSlider, mKneeFlowrateNumeric);
        }

        private void mHipMotorCurrentSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mHipMotorCurrentSlider.Value;
            mTelem.MotorTelem.Current[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipSliderChanged(value, mHipMotorCurrentSlider, mHipMotorCurrentNumeric, mKneeMotorCurrentSlider, mKneeMotorCurrentNumeric);
        }

        private void mHipMotorCurrentNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mHipMotorCurrentNumeric.Value;
            mTelem.MotorTelem.Current[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Hip] = value;

            HipNumericChanged(value, mHipMotorCurrentSlider, mHipMotorCurrentNumeric, mKneeMotorCurrentSlider, mKneeMotorCurrentNumeric);
        }

        private void mKneeMotorCurrentSlider_ValueChanged(object sender, EventArgs e)
        {
            int value = mKneeMotorCurrentSlider.Value;
            mTelem.MotorTelem.Current[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeSliderChanged(value, mKneeMotorCurrentSlider, mKneeMotorCurrentNumeric);
        }

        private void mKneeMotorCurrentNumeric_ValueChanged(object sender, EventArgs e)
        {
            int value = (int)mKneeMotorCurrentNumeric.Value;
            mTelem.MotorTelem.Current[(int)ProsthesisTelemetry.ProsthesisMotorTelemetry.HydraulicSystems.Knee] = value;

            KneeNumericChanged(value, mKneeMotorCurrentSlider, mKneeMotorCurrentNumeric);
        }

        private void HipSliderChanged(int toValue, TrackBar hipSlider, NumericUpDown hipNumericUpDown, TrackBar kneeSlider, NumericUpDown kneeNumericUpDown)
        {
            hipNumericUpDown.Value = toValue;

            if (mMirroringValues)
            {
                kneeSlider.Value = toValue;
                kneeNumericUpDown.Value = toValue;
            }
        }

        private void HipNumericChanged(int toValue, TrackBar hipSlider, NumericUpDown hipNumericUpDown, TrackBar kneeSlider, NumericUpDown kneeNumericUpDown)
        {
            if (hipSlider.Value != toValue)
            {
                hipSlider.Value = toValue;
            }

            if (mMirroringValues)
            {
                kneeSlider.Value = toValue;
                kneeNumericUpDown.Value = toValue;
            }
        }

        private void KneeSliderChanged(int toValue, TrackBar kneeSlider, NumericUpDown kneeNumericUpDown)
        {
            kneeNumericUpDown.Value = toValue;
        }

        private void KneeNumericChanged(int toValue, TrackBar kneeSlider, NumericUpDown kneeNumericUpDown)
        {
            if (kneeSlider.Value != toValue)
            {
                kneeSlider.Value = toValue;
            }
        }
    }
}

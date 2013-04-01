using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

using ProsthesisCore.Utility;

namespace ProsthesisClient
{
    public class ProsthesisTelemetryReceiver
    {
        public event Action<ProsthesisCore.Telemetry.ProsthesisTelemetry> Received = null;

        private ProsthesisCore.ProsthesisPacketParser mParser = new ProsthesisCore.ProsthesisPacketParser();
        private UdpClient mUDPReceiver = null;
        private System.Threading.Thread mTelemetryReceiver = null;
        private Logger mLogger = null;

        private bool mRunning = false;

        public ProsthesisTelemetryReceiver(Logger logger)
        {
            mLogger = logger;
            mUDPReceiver = new UdpClient(ProsthesisCore.ProsthesisConstants.kTelemetryPort);
            mUDPReceiver.JoinMulticastGroup(IPAddress.Parse(ProsthesisCore.ProsthesisConstants.kMulticastGroupAddress));
            mTelemetryReceiver = new System.Threading.Thread(RunThread);
        }

        public void Start()
        {
            if (!mRunning && mTelemetryReceiver != null && !mTelemetryReceiver.IsAlive)
            {
                mRunning = true;
                mTelemetryReceiver.Start();
            }
        }

        public void Stop()
        {
            if (mRunning && mTelemetryReceiver != null && mTelemetryReceiver.IsAlive)
            {
                mRunning = false;
                mTelemetryReceiver.Abort();
                mTelemetryReceiver = null;
                mUDPReceiver.Close();
            }
        }

        private void RunThread()
        {
            while (mRunning)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = mUDPReceiver.Receive(ref ipEndPoint);
                mParser.AddData(data, data.Length);
                while (mParser.MoveNext())
                {
                    ProsthesisCore.Messages.ProsthesisMessage msg = mParser.Current;
                    if (msg is ProsthesisCore.Telemetry.ProsthesisTelemetry)
                    {
                        if (Received != null)
                        {
                            Received(msg as ProsthesisCore.Telemetry.ProsthesisTelemetry);
                        }
                    }
                    else
                    {
                        mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Telemetry receiver caught an unexpected message type {0}", msg.GetType()));
                    }
                }
            }
        }
    }
}

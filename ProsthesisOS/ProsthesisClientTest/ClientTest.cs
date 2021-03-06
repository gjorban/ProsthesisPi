﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.Threading;

using ProsthesisCore.Utility;
using ProsthesisCore.Messages;

namespace ProsthesisClientTest
{
    sealed class ClientTest
    {
        private static Logger mLogger = null;
        private static Logger mTelemetryLogger = null;

        private static ProsthesisCore.ProsthesisPacketParser mPacketParser = new ProsthesisCore.ProsthesisPacketParser();
        private static ProsthesisClient.ProsthesisTelemetryReceiver mTelemReceiver = null;

        private static ManualResetEvent mWaitForConnect = new ManualResetEvent(false);
        private static ProsthesisSocketClient mClient = null;

        static void Main(string[] args)
        {
            string timestamp = System.DateTime.Now.ToString("dd MMM yyyy HH-mm-ss");
            string fileName = string.Format("ClientTest-{0}.txt", timestamp);
            string telemetryFileName = string.Format("ClientTest-Telemetry-{0}.txt", timestamp);

            mLogger = new Logger(fileName, true);
            //No no telemetry in main output
            mLogger.DeactivateChannels(Logger.LoggerChannels.Telemetry);

            //Start telemetry logging
            mTelemetryLogger = new Logger(telemetryFileName, false);
            mTelemReceiver = new ProsthesisClient.ProsthesisTelemetryReceiver(mTelemetryLogger);
            mTelemReceiver.Received += OnTelemetryReceive;

            mClient = new ProsthesisSocketClient(OnDataPacketReceive, "127.0.0.1", ProsthesisCore.ProsthesisConstants.ConnectionPort, mLogger);
            mClient.ConnectFinished += new Action<ProsthesisSocketClient, bool>(OnConnectFinished);
            mClient.ConnectionClosed += new Action<ProsthesisSocketClient>(OnConnectionClosed);

            mTelemReceiver.Start();

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                try
                {
                    if (mClient != null && mClient.Connected)
                    {
                        mClient.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Exception shutting down socket client: {0}", ex));
                }

                if (mTelemReceiver != null)
                {
                    mTelemReceiver.Stop();
                }

                mTelemetryLogger.ShutDown();
                mLogger.ShutDown();
            };

            try
            {
                mClient.StartConnect();
            }
            catch (SocketException ex)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Couldn't connect. Reason: {0}", ex));
            }
            finally
            {
                mLogger.LogMessage("Waiting for connection to server");
                mWaitForConnect.WaitOne();
                
                ProsthesisCore.Messages.ProsthesisHandshakeRequest req = new ProsthesisCore.Messages.ProsthesisHandshakeRequest();
                req.VersionId = ProsthesisCore.ProsthesisConstants.OSVersion;

                if (mClient.Connected)
                {
                    ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeRequest>(req);
                    mClient.Send(packet.Bytes, 0, packet.Bytes.Length);

                    ConsoleKey key = ConsoleKey.A;
                    do
                    {
                        while (mPacketParser.MoveNext())
                        {
                            ProsthesisMessage mess = mPacketParser.Current;
                            if (mess is ProsthesisHandshakeResponse)
                            {
                                ProsthesisHandshakeResponse castMess = mess as ProsthesisHandshakeResponse;
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got handshake response. Authed? {0}", castMess.AuthorizedConnection ? "yes" : "no"));
                            }
                            else if (mess is ProsthesisCommandAck)
                            {
                                ProsthesisCommandAck ack = mess as ProsthesisCommandAck;
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got command acknowledgement for cmd {0}", ack.Command));
                            }
                            else if (mess == null)
                            {
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got a null message from packet parser"));
                            }
                            else
                            {
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got unhandled message type: {0}", mess.GetType()));
                            }
                        }

                        //Check 60 times per second for messages (simulating a game loop)
                        System.Threading.Thread.Sleep(16);

                        if (Console.KeyAvailable)
                        {
                            key = Console.ReadKey().Key;

                            switch (key)
                            {
                                case ConsoleKey.I:
                                    SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Initialize);
                                    break;
                                case ConsoleKey.S:
                                    SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown);
                                    break;
                                case ConsoleKey.R:
                                    SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Resume);
                                    break;
                                case ConsoleKey.P:
                                    SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Pause);
                                    break;
                                case ConsoleKey.E:
                                    SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand.EmergencyStop);
                                    break;
                            }
                        }

                    } while (key != ConsoleKey.X && mClient.Connected);
                    mClient.Shutdown();
                }
                else
                {
                    ConsoleKey key = ConsoleKey.K;
                    do
                    {
                        System.Threading.Thread.Sleep(16);
                        if (Console.KeyAvailable)
                        {
                            key = Console.ReadKey().Key;
                        }
                    }
                    while (key != ConsoleKey.X);
                }

                mClient = null;
            }

            mTelemReceiver.Stop();
            mTelemetryLogger.ShutDown();
            mLogger.ShutDown();
        }

        private static void SendCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command)
        {
            if (mClient != null && mClient.Connected)
            {
                ProsthesisCommand cmd = new ProsthesisCommand();
                cmd.Command = command;

                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Sending command {0}", command));

                ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisCommand>(cmd);
                mClient.Send(packet.Bytes, 0, packet.Bytes.Length);
            }
        }

        #region Event Receivers
        private static void OnDataPacketReceive(ProsthesisSocketClient source, byte[] data, int len)
        {
            mPacketParser.AddData(data, len);
        }

        private static void OnTelemetryReceive(ProsthesisCore.Telemetry.ProsthesisTelemetry msg)
        {
            mTelemetryLogger.LogMessage(Logger.LoggerChannels.Telemetry, msg.ToString());
        }

        private static void OnConnectionClosed(ProsthesisSocketClient obj)
        {

        }

        private static void OnConnectFinished(ProsthesisSocketClient arg1, bool arg2)
        {
            mWaitForConnect.Set();
        }
        #endregion
    }
}

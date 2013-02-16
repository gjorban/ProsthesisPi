using System;
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
        private static ProsthesisCore.ProsthesisPacketParser mPacketParser = new ProsthesisCore.ProsthesisPacketParser();

        private static ManualResetEvent mWaitForConnect = new ManualResetEvent(false);

        private static void OnDataPacketReceive(ProsthesisSocketClient source, byte[] data, int len)
        {
            mPacketParser.AddData(data, len);
        }

        static void Main(string[] args)
        {
            string fileName = string.Format("ClientTest-{0}.txt", System.DateTime.Now.ToString("dd MMM yyyy HH-mm-ss"));
            mLogger = new Logger(fileName, true);
            ProsthesisSocketClient client = new ProsthesisSocketClient(OnDataPacketReceive, "127.0.0.1", ProsthesisCore.ProsthesisConstants.ConnectionPort, mLogger);
            client.ConnectFinished += new Action<ProsthesisSocketClient, bool>(OnConnectFinished);
            client.ConnectionClosed += new Action<ProsthesisSocketClient>(OnConnectionClosed);

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                try
                {
                    if (client != null && client.Connected)
                    {
                        client.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Exception shutting down socket client: {0}", ex));
                }

                mLogger.ShutDown();
            };

            try
            {
                client.StartConnect();
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

                if (client.Connected)
                {
                    ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeRequest>(req);
                    client.Send(packet.Bytes, 0, packet.Bytes.Length);

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
                            else if (mess == null)
                            {
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got a null message from packet parser"));
                            }
                            else
                            {
                                mLogger.LogMessage(Logger.LoggerChannels.Events, string.Format("Got unhandled message type: {0}", mess.GetType()));
                            }
                        }

                        //Check 60 times per second for messages (similar to game loop)
                        System.Threading.Thread.Sleep(16);

                        if (Console.KeyAvailable)
                        {
                            key = Console.ReadKey().Key;
                        }

                    } while (key != ConsoleKey.X);
                    client.Shutdown();
                }

                client = null;
            }

            mLogger.ShutDown();
        }

        static void OnConnectionClosed(ProsthesisSocketClient obj)
        {

        }

        static void OnConnectFinished(ProsthesisSocketClient arg1, bool arg2)
        {
            mWaitForConnect.Set();
        }
    }
}

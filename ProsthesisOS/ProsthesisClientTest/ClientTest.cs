using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;

using ProsthesisCore.Utility;
using ProsthesisCore.Messages;

namespace ProsthesisClientTest
{
    sealed class ClientTest
    {
        private static Logger mLogger = null;
        static void Main(string[] args)
        {
            string fileName = string.Format("ClientTest-{0}.txt", System.DateTime.Now.ToString("dd MMM yyyy HH-mm-ss"));
            mLogger = new Logger(fileName, true);
            TcpClient client = new TcpClient();

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                if (client.Connected)
                {
                    client.Close();
                }
                mLogger.ShutDown();
            };

            ProsthesisCore.ProsthesisPacketParser packetParser = new ProsthesisCore.ProsthesisPacketParser();

            try
            {
                client.Connect("127.0.0.1", ProsthesisCore.ProsthesisConstants.ConnectionPort);
            }
            catch (SocketException ex)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Couldn't connect. Reason: {0}", ex));
            }
            finally
            {
                if (client.Connected)
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Connected to server!"));
                    NetworkStream stream = client.GetStream();
                    stream.Flush();
                    ProsthesisCore.Messages.ProsthesisHandshakeRequest req = new ProsthesisCore.Messages.ProsthesisHandshakeRequest();
                    req.VersionId = ProsthesisCore.ProsthesisConstants.OSVersion;

                    System.Threading.Thread.Sleep(1000);
                   // for (int i = 0; i < 100; ++i)
                    {
                        ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeRequest>(req);                       
                        stream.Write(packet.Bytes, 0, packet.Bytes.Length);
                    }

                    ConsoleKey key;
                    do
                    {
                        key = Console.ReadKey().Key;

                        while (stream.DataAvailable)
                        {
                            byte[] buff = new byte[1024];
                            int readCount = stream.Read(buff, 0, 1024);
                            if (readCount > 0)
                            {
                                packetParser.AddData(buff, readCount);

                                try
                                {
                                    while (packetParser.MoveNext())
                                    {
                                        ProsthesisCore.Messages.ProsthesisMessage msg = packetParser.Current;
                                        if (msg != null)
                                        {
                                            if (msg is ProsthesisCore.Messages.ProsthesisHandshakeResponse)
                                            {
                                                ProsthesisCore.Messages.ProsthesisHandshakeResponse hsResp = msg as ProsthesisCore.Messages.ProsthesisHandshakeResponse;
                                                mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Got response. Auth is {0}", hsResp.AuthorizedConnection));
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Caught proto exception {0}", e));
                                }
                            }
                        }

                    } while (key != ConsoleKey.X);
                    client.Close();
                }
            }

            mLogger.ShutDown();
        }
    }
}

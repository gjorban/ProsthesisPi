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
        private class ConnectionState
        {
            public TcpClient Client = null;
            public byte[] Buffer = null;
        }

        private static Logger mLogger = null;
        private static ProsthesisCore.ProsthesisPacketParser mPacketParser = new ProsthesisCore.ProsthesisPacketParser();

        #region Data Callbacks
        private static AsyncCallback mConnectionCallback = null;
        private static AsyncCallback mDataReadyCallback = null;
        private static WaitCallback mDataWaitCallback = null;

        private static ManualResetEvent mWaitForConnect = new ManualResetEvent(false);

        private static void StartDataConnection(object state)
        {
            ConnectionState st = state as ConnectionState;
            st.Client.Client.BeginReceive(st.Buffer, 0, 0, SocketFlags.None, mDataReadyCallback, st);
        }

        private static void ConnectionReady_Handler(IAsyncResult ar)
        {
            try
            {
                ConnectionState st = ar.AsyncState as ConnectionState;
                st.Client.EndConnect(ar);
                if (st.Client.Connected)
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Connected to server!"));
                    //Queue the rest of the job to be executed latter
                    ThreadPool.QueueUserWorkItem(mDataWaitCallback, st);
                }
                else
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Unable to connect to {0}", st.Client.Client.RemoteEndPoint.AddressFamily.ToString()));
                }
            }
            catch (Exception e)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Connection exception: {0}", e));
            }
            finally
            {
                mWaitForConnect.Set();
            }
        }

        private static void DataReady_Handler(IAsyncResult ar)
        {
            ConnectionState st = ar.AsyncState as ConnectionState;
            try
            {
                if (st.Client == null || !st.Client.Connected)
                {
                    return;
                }
                st.Client.Client.EndReceive(ar);
            }
            catch
            {
                //DropConnection(st);
                return;
            }

            //Receive data here
            NetworkStream stream = st.Client.GetStream();
            while (stream.DataAvailable)
            {
                byte[] buff = new byte[1024];
                int readCount = stream.Read(buff, 0, 1024);
                if (readCount > 0)
                {
                    mPacketParser.AddData(buff, readCount);

                    try
                    {
                        while (mPacketParser.MoveNext())
                        {
                            ProsthesisCore.Messages.ProsthesisMessage msg = mPacketParser.Current;
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

            if (!st.Client.Connected)
            {
                return;
            }
            else
            {
                st.Client.Client.BeginReceive(st.Buffer, 0, 0, SocketFlags.None,
                    mDataReadyCallback, st);
            }
        }
        #endregion

        static void Main(string[] args)
        {
            string fileName = string.Format("ClientTest-{0}.txt", System.DateTime.Now.ToString("dd MMM yyyy HH-mm-ss"));
            mLogger = new Logger(fileName, true);
            TcpClient client = new TcpClient();
            ConnectionState state = new ConnectionState();
            state.Client = client;
            state.Buffer = new byte[4];

            mConnectionCallback = new AsyncCallback(ConnectionReady_Handler);
            mDataReadyCallback = new AsyncCallback(DataReady_Handler);
            mDataWaitCallback = new WaitCallback(StartDataConnection);

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                try
                {
                    if (client != null && client.Connected)
                    {
                        client.Close();
                    }
                }
                catch { }

                mLogger.ShutDown();
            };

            try
            {
                client.BeginConnect("127.0.0.1", ProsthesisCore.ProsthesisConstants.ConnectionPort, mConnectionCallback, state);
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
                    client.Client.Send(packet.Bytes, 0, packet.Bytes.Length, SocketFlags.None);

                    ConsoleKey key;
                    do
                    {
                        key = Console.ReadKey().Key;

                    } while (key != ConsoleKey.X);
                    client.Close();
                }

                client = null;
            }

            mLogger.ShutDown();
        }
    }
}

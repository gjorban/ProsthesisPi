using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore;
using ProsthesisCore.Utility;
using ProsthesisCore.Messages;

namespace ProsthesisOS.TCP
{
    sealed class ProsthesisSocketHandler : TcpServiceProvider
    {
        public delegate void MessageAvailableDelegate(ProsthesisCore.Messages.ProsthesisMessage message, ConnectionState state);
        public event MessageAvailableDelegate MessageAvailable = null;

        public delegate void SocketConnectionDelegate(ConnectionState state);
        public event SocketConnectionDelegate Connection = null;
        public event SocketConnectionDelegate Disconnection = null;

        private Dictionary<ConnectionState, ProsthesisCore.ProsthesisPacketParser> mParsers = new Dictionary<ConnectionState, ProsthesisPacketParser>();
        private System.IO.MemoryStream mReceiveBuffer = new System.IO.MemoryStream(1024);

        private Logger mLogger
        {
            get { return ProsthesisOS.Program.Logger; }
        }

        public override object Clone()
        {
            ProsthesisSocketHandler handler = new ProsthesisSocketHandler();
            handler.Connection = Connection;
            handler.Disconnection = Disconnection;
            handler.MessageAvailable = MessageAvailable;
            return handler;
        }

        public override void OnAcceptConnection(ConnectionState state)
        {
            mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Received connection from {0}", state.RemoteEndPoint));
            if (!mParsers.ContainsKey(state))
            {
                mParsers[state] = new ProsthesisPacketParser();
            }

            if (Connection != null)
            {
                Connection(state);
            }
        }

        public override void OnDropConnection(ConnectionState state)
        {
            mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Dropped connection from {0}", state.RemoteEndPoint));
            mParsers.Remove(state);

            if (Disconnection != null)
            {
                Disconnection(state);
            }
        }

        public override void OnReceiveData(ConnectionState state)
        {
            byte[] buffer = new byte[1024];
            while (state.AvailableData > 0)
            {
                int readCount = state.Read(buffer, 0, (int)(mReceiveBuffer.Capacity - mReceiveBuffer.Position));
                if (readCount > 0)
                {
                    //Drop connection if we are missing a packet parser
                    if (!mParsers.ContainsKey(state))
                    {
                        mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Dropped connection to {0} because its parser was missing.", GetIPFor(state.RemoteEndPoint)));
                        state._server.DropConnection(state);
                        return;
                    }

                    ProsthesisPacketParser parser = mParsers[state];
                    parser.AddData(buffer, readCount);
                    try
                    {
                        while (parser.MoveNext())
                        {
                            ProsthesisMessage msg = parser.Current;
                          /*  if (msg == null)
                            {
                                mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Failed to parse packet from {0}. Dropping connection", GetIPFor(state.RemoteEndPoint)));
                                state._server.DropConnection(state);
                            }
                            else if (msg is ProsthesisHandshakeRequest)
                            {
                                ProsthesisHandshakeRequest hsR = msg as ProsthesisHandshakeRequest;

                                mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Received handshake w/ Version ID {0} from {1}", hsR.VersionId, GetIPFor(state.RemoteEndPoint)));

                                ProsthesisHandshakeResponse hsResp = new ProsthesisHandshakeResponse();
                                if (hsR.VersionId == ProsthesisCore.ProsthesisConstants.OSVersion)
                                {
                                    hsResp.AuthorizedConnection = true;
                                    hsResp.ErrorString = string.Empty;
                                    ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisCore.Messages.ProsthesisHandshakeResponse>(hsResp);

                                    state.Write(packet.Bytes, 0, (int)packet.Bytes.Length);
                                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Client version equals ours({0}). Accepting connection", ProsthesisCore.ProsthesisConstants.OSVersion));
                                }
                                else
                                {
                                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Client version ID doesn't match ours(ours: {0}. Theirs: {1}). Rejecting Connection from {2}", 
                                        ProsthesisCore.ProsthesisConstants.OSVersion, 
                                        hsR.VersionId,
                                        GetIPFor(state.RemoteEndPoint)));

                                    hsResp.AuthorizedConnection = false;
                                    hsResp.ErrorString = string.Format("Version mismatch. Server is {0} but client sent {1}", ProsthesisCore.ProsthesisConstants.OSVersion, hsR.VersionId);

                                    ProsthesisDataPacket packet = ProsthesisDataPacket.BoxMessage<ProsthesisCore.Messages.ProsthesisHandshakeResponse>(hsResp);

                                    state.Write(packet.Bytes, 0, (int)packet.Bytes.Length);
                                    state._server.DropConnection(state);
                                    return;
                                }
                            }*/
                            if (MessageAvailable != null)
                            {
                                MessageAvailable(msg, state);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Caught protobuf exception {0} receiving data from {1}. Closing connection", e, GetIPFor(state.RemoteEndPoint)));
                        state._server.DropConnection(state);
                    }
                }
                else
                {
                    //If read fails then close connection
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Read failed from {0}. Dropping connection", GetIPFor(state.RemoteEndPoint)));
                    state._server.DropConnection(state);
                }
            }
        }

        private static string GetIPFor(System.Net.EndPoint endPoint)
        {
            return ((System.Net.IPEndPoint)endPoint).Address.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore;

using TcpLib;

namespace ProsthesisOS.TCP
{
    sealed class HandshakeService : TcpServiceProvider
    {
        public override object Clone()
        {
            return new HandshakeService();
        }

        public override void OnAcceptConnection(ConnectionState state)
        {
            Console.WriteLine(string.Format("Received connection from {0}", state.RemoteEndPoint));
        }

        public override void OnDropConnection(ConnectionState state)
        {
            Console.WriteLine(string.Format("Dropped connection from {0}", state.RemoteEndPoint));
        }

        private System.IO.MemoryStream mReceiveBuffer = new System.IO.MemoryStream(1024);
        private ProsthesisCore.ProsthesisPacketParser mParser = new ProsthesisPacketParser();

        public override void OnReceiveData(ConnectionState state)
        {
            byte[] buffer = new byte[1024];
            while (state.AvailableData > 0)
            {
                int readCount = state.Read(buffer, 0, (int)(mReceiveBuffer.Capacity - mReceiveBuffer.Position));
                if (readCount > 0)
                {
                    mParser.AddData(buffer, readCount);
                    try
                    {
                        while (mParser.MoveNext())
                        {
                            ProsthesisCore.Messages.ProsthesisMessage msg = mParser.Current;
                            if (msg is ProsthesisCore.Messages.ProsthesisHandshakeRequest)
                            {
                                ProsthesisCore.Messages.ProsthesisHandshakeRequest hsR = msg as ProsthesisCore.Messages.ProsthesisHandshakeRequest;

                                Console.WriteLine(string.Format("Received handshake w/ Version ID {0}", hsR.VersionId));
                            }
                        }
                     /*   ProsthesisCore.Messages.ProsthesisMessage msg = ProtoBuf.Serializer.DeserializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisMessage>(mReceiveBuffer, ProtoBuf.PrefixStyle.Fixed32);

                        if (msg != null)
                        {
                            if (msg is ProsthesisCore.Messages.ProsthesisHandshakeRequest)
                            {
                                ProsthesisCore.Messages.ProsthesisHandshakeRequest hsR = msg as ProsthesisCore.Messages.ProsthesisHandshakeRequest;

                                Console.WriteLine(string.Format("Received handshake w/ Version ID {0}", hsR.VersionId));

                                ProsthesisCore.Messages.ProsthesisHandshakeResponse hsResp = new ProsthesisCore.Messages.ProsthesisHandshakeResponse();

                                if (hsR.VersionId == ProsthesisCore.ProsthesisConstants.OSVersion)
                                {
                                    hsResp.AuthorizedConnection = true;
                                    hsResp.ErrorString = string.Empty;
                                    System.IO.MemoryStream outBuff = new System.IO.MemoryStream();
                                    ProtoBuf.Serializer.SerializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisHandshakeResponse>(outBuff, hsResp, ProtoBuf.PrefixStyle.Fixed32);

                                    state.Write(outBuff.ToArray(), 0, (int)outBuff.Length);
                                    Console.WriteLine(string.Format("Client version equals ours({0}). Accepting connection", ProsthesisCore.ProsthesisConstants.OSVersion));
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Client version ID doesn't match ours(ours: {0}. Theirs: {1}). Rejecting Connection", ProsthesisCore.ProsthesisConstants.OSVersion, hsR.VersionId));
                                    hsResp.AuthorizedConnection = false;
                                    hsResp.ErrorString = string.Format("Version mismatch. Server is {0} but client sent {1}", ProsthesisCore.ProsthesisConstants.OSVersion, hsR.VersionId);

                                    System.IO.MemoryStream outBuff = new System.IO.MemoryStream();
                                    ProtoBuf.Serializer.SerializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisHandshakeResponse>(outBuff, hsResp, ProtoBuf.PrefixStyle.Fixed32);

                                    state.Write(outBuff.ToArray(), 0, (int)outBuff.Length);

                                    state.EndConnection();
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine(string.Format("Got a message of type {0} but I don't understand it. Ignoring", msg.GetType()));
                            }
                        }*/
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(string.Format("Caught protobuf exception {0}", e));
                    }
                }
                else
                {
                    //If read fails then close connection
                    state.EndConnection(); 
                }
            }
        }
    }
}

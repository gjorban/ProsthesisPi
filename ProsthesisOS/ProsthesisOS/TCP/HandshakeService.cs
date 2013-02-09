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

        private List<byte> mBuffer = new List<byte>();

        public override void OnReceiveData(ConnectionState state)
        {
            byte[] buffer = new byte[1024];
            while (state.AvailableData > 0)
            {
                int readCount = state.Read(buffer, 0, 1024);
                if (readCount > 0)
                {
                    try
                    {
                        ProsthesisCore.Messages.ProsthesisMessage msg = ProtoBuf.Serializer.DeserializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisMessage>(new System.IO.MemoryStream(buffer), ProtoBuf.PrefixStyle.Fixed32);

                        if (msg != null && (msg is ProsthesisCore.Messages.ProsthesisHandshakeRequest))
                        {
                            Console.WriteLine(string.Format("Received handshake w/ Version ID {0}", (msg as ProsthesisCore.Messages.ProsthesisHandshakeRequest).VersionId));
                        }
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

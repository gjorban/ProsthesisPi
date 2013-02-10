using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;

namespace ProsthesisClientTest
{
    sealed class ClientTest
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect("127.0.0.1", ProsthesisCore.ProsthesisConstants.ConnectionPort);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(string.Format("Couldn't connect. Reason: {0}", ex));
            }
            finally
            {
                if (client.Connected)
                {
                    Console.WriteLine(string.Format("Connected to server!"));
                    NetworkStream stream = client.GetStream();
                    stream.Flush();
                    ProsthesisCore.Messages.ProsthesisHandshakeRequest req = new ProsthesisCore.Messages.ProsthesisHandshakeRequest();
                    req.VersionId = ProsthesisCore.ProsthesisConstants.OSVersion;

                    System.Threading.Thread.Sleep(1000);
                    for (int i = 0; i < 100; ++i)
                    {
                        byte[] data = new byte[1024];
                        System.IO.MemoryStream dataStream = new System.IO.MemoryStream(data);
                        ProtoBuf.Serializer.SerializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisHandshakeRequest>(dataStream, req, ProtoBuf.PrefixStyle.Fixed32);
                        int usedBytes = (int)dataStream.Position;

                        dataStream.Position = 0;

                        ProsthesisCore.Messages.ProsthesisDataPacket packet = new ProsthesisCore.Messages.ProsthesisDataPacket(dataStream.ToArray(), usedBytes);
                        byte[] packetData = packet.Bytes;
                        stream.Write(packetData, 0, packetData.Length);

                        System.Threading.Thread.Sleep(10);
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
                                System.IO.MemoryStream memStream = new System.IO.MemoryStream(buff);

                                try
                                {
                                    ProsthesisCore.Messages.ProsthesisMessage msg = ProtoBuf.Serializer.DeserializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisMessage>(memStream, ProtoBuf.PrefixStyle.Fixed32);
                                    if (msg != null)
                                    {
                                        if (msg is ProsthesisCore.Messages.ProsthesisHandshakeResponse)
                                        {
                                            ProsthesisCore.Messages.ProsthesisHandshakeResponse hsResp = msg as ProsthesisCore.Messages.ProsthesisHandshakeResponse;
                                            Console.WriteLine(string.Format("Got response. Auth is {0}", hsResp.AuthorizedConnection));
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(string.Format("Caught proto exception {0}", e));
                                }
                            }
                        }

                    } while (key != ConsoleKey.X);
                    client.Close();
                }
            }
        }
    }
}

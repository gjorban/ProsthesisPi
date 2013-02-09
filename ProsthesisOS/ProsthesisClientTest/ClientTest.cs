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
                    ProtoBuf.Serializer.SerializeWithLengthPrefix<ProsthesisCore.Messages.ProsthesisHandshakeRequest>(stream, req, ProtoBuf.PrefixStyle.Fixed32);

                    ConsoleKey key;
                    do
                    {
                        key = Console.ReadKey().Key;

                        int byteCnt = 0;
                        while (stream.DataAvailable)
                        {
                            byteCnt++;
                            stream.ReadByte();
                        }
                        Console.Write(string.Format("Got {0} bytes", byteCnt));

                    } while (key != ConsoleKey.X);
                    client.Close();
                }
            }
        }
    }
}

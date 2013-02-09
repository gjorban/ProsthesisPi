using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TcpLib;

namespace ProsthesisOS
{
    public static class Program
    {
        
        public static void Main(string[] args)
        {
            ProsthesisOS.TCP.HandshakeService echoSP = new ProsthesisOS.TCP.HandshakeService();
            TcpServer server = new TcpServer(echoSP, ProsthesisCore.ProsthesisConstants.ConnectionPort);

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                if (server.Active)
                {
                    server.Stop();
                }
            };

            if (server.Start())
            {
                Console.WriteLine(string.Format("Started server at {0}. Press 'x' to exit", ProsthesisCore.ProsthesisConstants.ConnectionPort));
                while (Console.ReadKey().Key != ConsoleKey.X) { }
                server.Stop();
            }
            else
            {
                Console.WriteLine("Couldn't start server");
                System.Threading.Thread.Sleep(1000);
            }
        }

        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

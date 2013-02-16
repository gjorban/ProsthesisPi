using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TcpLib;

namespace ProsthesisOS
{
    public static class Program
    {
        private static ProsthesisCore.Utility.Logger mLogger = null;
        public static ProsthesisCore.Utility.Logger Logger { get { return mLogger; } }

        public static void Main(string[] args)
        {
            string fileName = string.Format("Server-{0}.txt", System.DateTime.Now.ToString("dd MMM yyyy HH-mm-ss"));
            mLogger = new ProsthesisCore.Utility.Logger(fileName, true);

            mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.General, "ProsthesisOS startup", true);

            ProsthesisOS.TCP.HandshakeService echoSP = new ProsthesisOS.TCP.HandshakeService();
            TcpServer server = new TcpServer(echoSP, ProsthesisCore.ProsthesisConstants.ConnectionPort);

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                if (server.Active)
                {
                    server.Stop();
                }
                mLogger.ShutDown();
            };

            if (server.Start())
            {
                Logger.LogMessage(string.Format("Started server at {0}. Press 'x' to exit", ProsthesisCore.ProsthesisConstants.ConnectionPort));
                while (Console.ReadKey().Key != ConsoleKey.X) { }
                server.Stop();
            }
            else
            {
                Logger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Faults, "Couldn't start server");
                System.Threading.Thread.Sleep(1000);
            }

            mLogger.ShutDown();
        }
    }
}

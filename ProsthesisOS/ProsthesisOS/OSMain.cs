using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.TCP;

namespace ProsthesisOS
{
    public static class Program
    {
        private static ProsthesisCore.Utility.Logger mLogger = null;
        public static ProsthesisCore.Utility.Logger Logger { get { return mLogger; } }

        public static void Main(string[] args)
        {
            string fileName = string.Format("Server-{0}.txt", System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
            mLogger = new ProsthesisCore.Utility.Logger(fileName, true);
            mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.General, "ProsthesisOS startup", true);

            States.ProsthesisMainContext context = new States.ProsthesisMainContext(ProsthesisCore.ProsthesisConstants.ConnectionPort, mLogger);

            //Safely shut down app
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                if (context.IsRunning)
                {
                    context.Terminate("Aborted");
                }

                mLogger.ShutDown();
            };

            Console.WriteLine("Press 'x' to exit");

            while (Console.ReadKey().Key != ConsoleKey.X) { }

            mLogger.ShutDown();
        }
    }
}

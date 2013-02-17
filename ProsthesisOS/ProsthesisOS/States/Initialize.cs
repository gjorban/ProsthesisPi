using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Utility;

namespace ProsthesisOS.States
{
    internal class Initialize : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            if (context.TCPServer.Start())
            {
                context.Logger.LogMessage(Logger.LoggerChannels.StateMachine, "TCP Server started");
                return new States.WaitForConnection();
            }
            else
            {
                context.Logger.LogMessage(Logger.LoggerChannels.StateMachine, "TCP server failed to start");
                return new States.Shutdown();
            }
        }

        public override void OnExit(ProsthesisContext context)
        {

        }
    }
}

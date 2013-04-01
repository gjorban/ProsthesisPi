using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Utility;
using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class Initialize : ProsthesisStateBase
    {
        public Initialize(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            if (mContext.TCPServer.Start())
            {
                mContext.Logger.LogMessage(Logger.LoggerChannels.StateMachine, "TCP Server started");
                return new States.OperationalSuperState(mContext);
            }
            else
            {
                mContext.Logger.LogMessage(Logger.LoggerChannels.StateMachine, "TCP server failed to start");
                return new States.Shutdown(mContext);
            }
        }

        public override void OnExit()
        {

        }
    }
}

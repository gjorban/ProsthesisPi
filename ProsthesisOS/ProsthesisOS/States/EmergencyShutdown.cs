using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class EmergencyShutdown : ProsthesisStateBase
    {
        public EmergencyShutdown(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            return this;
        }

        public override void OnExit()
        {

        }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
                case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Initialize:

                    break;

                case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                    return new Shutdown(mContext);
            }
            return this;
        }
    }
}

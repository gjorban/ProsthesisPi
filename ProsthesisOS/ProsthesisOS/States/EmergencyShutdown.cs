using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States
{
    internal class EmergencyShutdown : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            return this;
        }

        public override void OnExit(ProsthesisContext context)
        {

        }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisContext context, ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
                case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Initialize:

                    break;

                case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                    return new Shutdown();
            }
            return this;
        }
    }
}

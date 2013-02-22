using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States
{
    internal abstract class ProsthesisStateBase
    {
        public abstract ProsthesisStateBase OnEnter(ProsthesisContext context);
        public abstract void OnExit(ProsthesisContext context);

        public virtual ProsthesisStateBase OnConnection(ProsthesisContext context, TCP.ConnectionState state)
        {
            return this;
        }

        public virtual ProsthesisStateBase OnDisconnection(ProsthesisContext context, TCP.ConnectionState state)
        {
            if (state == context.AuthorizedConnection)
            {
                return new Shutdown();
            }
            return this;
        }

        public virtual ProsthesisStateBase OnClientAuthorization(ProsthesisContext context, TCP.ConnectionState authedClient, bool isAuthorized)
        {
            return this;
        }

        public virtual ProsthesisStateBase OnFault(ProsthesisContext context, string faultDescription)
        {
            return new EmergencyShutdown();
        }

        public virtual ProsthesisStateBase OnProsthesisCommand(ProsthesisContext context, ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                return new Shutdown();
            default:
                return this;
            }
        }

        public virtual ProsthesisStateBase OnSocketMessage(ProsthesisContext context, ProsthesisCore.Messages.ProsthesisMessage message, TCP.ConnectionState state)
        {
            return this;
        }
    }
}

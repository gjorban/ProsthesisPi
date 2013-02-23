using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States.Base
{
    internal abstract class ProsthesisStateBase
    {
        protected IProsthesisContext mContext = null;
        protected ProsthesisStateBase(IProsthesisContext context)
        {
            mContext = context;
        }

        public abstract ProsthesisStateBase OnEnter();
        public abstract void OnExit();

        public virtual ProsthesisStateBase OnConnection(TCP.ConnectionState state)
        {
            return this;
        }

        public virtual ProsthesisStateBase OnDisconnection(TCP.ConnectionState state)
        {
            if (state == mContext.AuthorizedConnection)
            {
                return new Shutdown(mContext);
            }
            return this;
        }

        public virtual ProsthesisStateBase OnClientAuthorization(TCP.ConnectionState authedClient, bool isAuthorized)
        {
            return this;
        }

        public virtual ProsthesisStateBase OnFault(string faultDescription)
        {
            return new EmergencyShutdown(mContext);
        }

        public virtual ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                return new Shutdown(mContext);
            default:
                return this;
            }
        }

        public virtual ProsthesisStateBase OnSocketMessage(ProsthesisCore.Messages.ProsthesisMessage message, TCP.ConnectionState state)
        {
            return this;
        }
    }
}

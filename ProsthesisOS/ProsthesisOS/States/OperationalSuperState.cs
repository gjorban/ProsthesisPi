using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Utility;

namespace ProsthesisOS.States
{
    internal class OperationalSuperState : ProsthesisStateBase
    {
        private ProsthesisStateBase mCurrentState = null;
        private ProsthesisContext mContext = null;
        private ProsthesisCore.Utility.Logger mLogger = null;

        public OperationalSuperState(Logger logger)
        {
            mLogger = logger;
        }

        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            mContext = context;
            ProsthesisStateBase initialState = new WaitForBootup();
            ChangeState(initialState);

            return this;
        }

        public override void OnExit(ProsthesisContext context)
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnExit(context);
            }
        }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisContext context, ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                return new Shutdown();
            default:
                {
                    ProsthesisStateBase newState =  mCurrentState.OnProsthesisCommand(context, command, from);
                    if (newState != mCurrentState)
                    {
                        ChangeState(newState);
                    }
                }
                break;
            }

            return this;
        }

        public override ProsthesisStateBase OnSocketMessage(ProsthesisContext context, ProsthesisCore.Messages.ProsthesisMessage message, TCP.ConnectionState state)
        {
            ProsthesisStateBase newState = mCurrentState.OnSocketMessage(context, message, state);
            if (newState != mCurrentState)
            {
                ChangeState(newState);
            }
            return this;
        }

        private void ChangeState(ProsthesisStateBase to)
        {
            if (to != mCurrentState)
            {
                if (mCurrentState != null)
                {
                    mCurrentState.OnExit(mContext);
                }
                ProsthesisStateBase oldState = mCurrentState;
                mCurrentState = to;
                ProsthesisStateBase chainedState = to.OnEnter(mContext);

                mLogger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("Changing sub-state of {2} state from {0} to {1}",
                    oldState != null ? oldState.GetType().ToString() : "<none>",
                    to != null ? to.GetType().ToString() : "<none>", GetType()));

                ChangeState(chainedState);
            }
        }
    }
}

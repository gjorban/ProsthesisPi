using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Utility;
using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class OperationalSuperState : ProsthesisStateBase, IProsthesisContext
    {
        public ProsthesisStateBase CurrentState { get { return mContext.CurrentState; } }
        public TCP.TcpServer TCPServer { get { return mContext.TCPServer; } }
        public TCP.ConnectionState AuthorizedConnection { get { return mContext.AuthorizedConnection; } }
        public Logger Logger { get { return mContext.Logger; } }
        public bool IsRunning { get { return mContext.IsRunning; } }

        private ProsthesisStateBase mCurrentState = null;

        public OperationalSuperState(IProsthesisContext context) : base(context) { }

        #region ProsthesisStateBase Impl
        public override ProsthesisStateBase OnEnter()
        {
            ProsthesisStateBase initialState = new WaitForBootup(this);
            ChangeState(initialState);

            return this;
        }

        public override void OnExit()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnExit();
            }
        }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                return new Shutdown(this);

            default:
                {
                    ProsthesisStateBase newState =  mCurrentState.OnProsthesisCommand(command, from);
                    if (newState != mCurrentState)
                    {
                        ChangeState(newState);
                    }
                }
                break;
            }

            return this;
        }

        public override ProsthesisStateBase OnSocketMessage(ProsthesisCore.Messages.ProsthesisMessage message, TCP.ConnectionState state)
        {
            ProsthesisStateBase newState = mCurrentState.OnSocketMessage(message, state);
            if (newState != mCurrentState)
            {
                ChangeState(newState);
            }
            return this;
        }
        #endregion

        #region IProsthesisContext Impl
        public void RaiseFault(string description)
        {
            Terminate(description);
        }

        public void Terminate(string reason)
        {
            //Exit our current state first
            ChangeState(null);
            mContext.Terminate(reason);
        }

        public void ChangeState(ProsthesisStateBase to)
        {
            if (to != mCurrentState)
            {
                if (mCurrentState != null)
                {
                    mCurrentState.OnExit();
                }
                ProsthesisStateBase oldState = mCurrentState;
                ProsthesisStateBase chainedState = null;

                if (to != null)
                {
                    chainedState = to.OnEnter();
                }
                mCurrentState = to;

                mContext.Logger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("Changing sub-state of {2} state from {0} to {1}",
                    oldState != null ? oldState.GetType().ToString() : "<none>",
                    to != null ? to.GetType().ToString() : "<none>", GetType()));

                if (chainedState != null && chainedState != mCurrentState)
                {
                    ChangeState(chainedState);
                }
            }
        }
        #endregion
    }
}

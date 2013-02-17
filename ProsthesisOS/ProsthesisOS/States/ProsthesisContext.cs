using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore;
using ProsthesisCore.Messages;
using ProsthesisCore.Utility;

namespace ProsthesisOS.States
{
    internal class ProsthesisContext
    {
        public ProsthesisStateBase CurrentState { get { return mCurrentState; } }
        public TCP.TcpServer TCPServer { get { return mSocketConnection; } }
        public TCP.ConnectionState AuthorizedConnection { get { return mAuthorizedConnection; } }
        public Logger Logger { get { return mLogger; } }
        public bool IsRunning { get { return mRunning; } }

        public delegate void ProsthesisStateChangeDelegate(ProsthesisStateBase from, ProsthesisStateBase to);
        public event ProsthesisStateChangeDelegate StateChanged = null;

        private ProsthesisStateBase mCurrentState = null;
        private TCP.TcpServer mSocketConnection = null;
        private TCP.ProsthesisSocketHandler mSocketServer = new TCP.ProsthesisSocketHandler();
        private Logger mLogger = null;
        private bool mRunning = true;

        private TCP.ConnectionState mAuthorizedConnection = null;
        private System.Threading.ManualResetEvent mMachineActiveWait = new System.Threading.ManualResetEvent(false);

        public ProsthesisContext(int tcpPort, Logger logger)
        {
            mSocketConnection = new TCP.TcpServer(mSocketServer, tcpPort);

            mSocketServer.Connection += OnConnection;
            mSocketServer.Disconnection += OnDisconnection;
            mSocketServer.MessageAvailable += OnSocketMessageAvailable;

            mLogger = logger;
            mLogger.LogMessage(Logger.LoggerChannels.StateMachine, "State machine initializing");
            mMachineActiveWait.Reset();
            ChangeState(new States.Initialize());
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void Terminate(string reason)
        {
            mLogger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("State machine terminated because: {0}", reason));
            mSocketConnection.Stop();
            mMachineActiveWait.Set();
            mRunning = false;
        }

        private void ChangeState(ProsthesisStateBase to)
        {
            ProsthesisStateBase oldState = mCurrentState;
            ProsthesisStateBase chainedState = null;

            if (mCurrentState != to)
            {
                if (mCurrentState != null)
                {
                    mCurrentState.OnExit(this);
                }

                if (to != null)
                {
                    chainedState = to.OnEnter(this);
                }
                mCurrentState = to;

                mLogger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("Changing state from {0} to {1}",
                    oldState != null ? oldState.GetType().ToString() : "<none>",
                    to != null ? to.GetType().ToString() : "<none>"));

                if (StateChanged != null)
                {
                    StateChanged(oldState, mCurrentState);
                }

                if (chainedState != null && chainedState != mCurrentState)
                {
                    ChangeState(chainedState);
                }
            }
        }

        #region Event Receivers
        private void OnConnection(TCP.ConnectionState state)
        {
            ProsthesisStateBase newState = mCurrentState.OnConnection(this, state);
            ChangeState(newState);
        }

        private void OnDisconnection(TCP.ConnectionState state)
        {
            ProsthesisStateBase newState = mCurrentState.OnDisconnection(this, state);
            ChangeState(newState);
        }

        private void OnSocketMessageAvailable(ProsthesisMessage message, TCP.ConnectionState state)
        {
            //Only one message allowed in at a time
            lock (this)
            {
                ProsthesisStateBase newState = mCurrentState;
                //Capture handshakes and send appropriate events
                if (message is ProsthesisHandshakeRequest)
                {
                    ProsthesisHandshakeRequest hsReq = message as ProsthesisHandshakeRequest;
                    if (hsReq.VersionId == ProsthesisCore.ProsthesisConstants.OSVersion)
                    {
                        newState = mCurrentState.OnClientAuthorization(this, state, true);
                        mAuthorizedConnection = state;
                    }
                    else
                    {
                        newState = mCurrentState.OnClientAuthorization(this, state, false);
                    }
                }
                else if (state == mAuthorizedConnection)
                {
                    //Capture commands and send appropriate events
                    if (message is ProsthesisCommand)
                    {
                        ProsthesisCommand command = message as ProsthesisCommand;
                        mLogger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("Received command {0} from {1}", command.Command, state.RemoteEndPoint));
                        newState = mCurrentState.OnProsthesisCommand(this, command.Command, state);
                    }
                    else
                    {
                        newState = mCurrentState.OnSocketMessage(this, message, state);
                    }
                }
                else
                {
                    state._server.DropConnection(state);
                }

                ChangeState(newState);
            }
        }
        #endregion
    }
}

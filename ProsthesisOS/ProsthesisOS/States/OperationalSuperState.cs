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
        private ProsthesisStateBase mDeferredStateChange = null;

        private ArduinoCommunicationsLibrary.MotorControllerArduino mMotorControllerArduino = null;
        private bool mRunning = false;

        public OperationalSuperState(IProsthesisContext context) : base(context) 
        {
            mMotorControllerArduino = new ArduinoCommunicationsLibrary.MotorControllerArduino(context.Logger);
            mMotorControllerArduino.TelemetryUpdate += UpdateMotorTelemetry;
        }

        #region ProsthesisStateBase Impl
        public override ProsthesisStateBase OnEnter()
        {
            mRunning = true;
            ArduinoCommunicationsLibrary.ArduinoCommsBase.InitializeSerialConnections(mContext.Logger);
            ProsthesisStateBase initialState = new WaitForBootup(this, new ArduinoCommunicationsLibrary.ArduinoCommsBase[] { mMotorControllerArduino });
            ChangeState(initialState);

            return this;
        }

        public override void OnExit()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnExit();
            }

            if (mMotorControllerArduino != null)
            {
                if (mMotorControllerArduino.TelemetryActive)
                {
                    mMotorControllerArduino.TelemetryToggle(0);
                }

                mMotorControllerArduino.StopArduinoComms(true);
                mMotorControllerArduino.TelemetryUpdate -= UpdateMotorTelemetry;
                mMotorControllerArduino = null;
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
            mRunning = false;
            if (mCurrentState != null)
            {
                mCurrentState.OnExit();
            }
            mContext.Terminate(reason);
        }

        public void ChangeState(ProsthesisStateBase to)
        {
            if (!mRunning)
            {
                return;
            }

            if (mDeferredStateChange != null)
            {
                mDeferredStateChange = to;
                return;
            }

            if (to != mCurrentState)
            {
                if (mCurrentState != null)
                {
                    mCurrentState.OnExit();
                }

                ProsthesisStateBase oldState = mCurrentState;

                if (to != null)
                {
                    mDeferredStateChange = to.OnEnter();
                }
                mCurrentState = to;

                if (mRunning)
                {
                    mContext.Logger.LogMessage(Logger.LoggerChannels.StateMachine, string.Format("Changing sub-state of {2} state from {0} to {1}",
                        oldState != null ? oldState.GetType().ToString() : "<none>",
                        to != null ? to.GetType().ToString() : "<none>", GetType()));
                }
            }

            if (mDeferredStateChange != null && mRunning)
            {
                ProsthesisStateBase nextState = mDeferredStateChange;
                mDeferredStateChange = null;
                ChangeState(nextState);
            }
        }

        public void UpdateMotorTelemetry(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry motorTelem)
        {
            mContext.UpdateMotorTelemetry(motorTelem);
        }
        #endregion

        #region Arduino Event Receivers
        private void OnArduinoStateChange(ArduinoCommunicationsLibrary.ArduinoCommsBase arduino, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState from, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState to)
        {
            if (to == ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Fault)
            {
                RaiseFault(string.Format("AID {0} reported a fault.", arduino.ArduinoID));
            }
        }
        #endregion
    }
}

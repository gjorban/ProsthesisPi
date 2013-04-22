using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class RunSelfTest : ProsthesisStateBase
    {
        private System.Timers.Timer mTimer = null;
        private ArduinoCommunicationsLibrary.ArduinoCommsBase[] mArduinos = null;

        public RunSelfTest(IProsthesisContext context, ArduinoCommunicationsLibrary.ArduinoCommsBase[] arduinos) : base(context) 
        {
            mArduinos = arduinos;
        }

        public override ProsthesisStateBase OnEnter()
        {
            mTimer = new System.Timers.Timer();
            mTimer.AutoReset = false;
            mTimer.Interval = 1000;
            mTimer.Elapsed += OnTimer;
            mTimer.Start();
            return this;
        }

        public override void OnExit()
        {
            if (mTimer != null)
            {
                mTimer.Stop();
            }
        }

        private void OnTimer(object source, System.Timers.ElapsedEventArgs e)
        {
            mContext.Logger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.System, "Timer elapsed! Should trigger next state!");
            //Make sure that our context is actually running before proceeding to IDLE
            if (mContext.IsRunning)
            {
                mContext.ChangeState(new ProsthesisIdle(mContext, mArduinos));
            }
        }
    }
}

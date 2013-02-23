using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class WaitForBootup : ProsthesisStateBase
    {
        private System.Timers.Timer mTimer = null;
        public WaitForBootup(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            mTimer = new System.Timers.Timer();
            mTimer.AutoReset = false;
            mTimer.Interval = 1000;
            mTimer.Elapsed += OnTimer;
            mTimer.Start();
            return this;
        }

        private void OnTimer(object source, System.Timers.ElapsedEventArgs e)
        {
            mContext.Logger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.System, "Timer elapsed! Should trigger next state!");
        }

        public override void OnExit()
        {
            if (mTimer != null)
            {
                mTimer.Stop();
            }
        }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Initialize:
                return new RunSelfTest(mContext);

            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Shutdown:
                return new Shutdown(mContext);
            }
            return this;
        }
    }
}

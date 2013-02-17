using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States
{
    internal class OperationalSuperState : ProsthesisStateBase
    {
        private ProsthesisStateBase mCurrentState = null;
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            mCurrentState = new WaitForBootup();

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
                
                break;
            }

            return this;
        }
        //TODO: Hook up all remaining pass-thrus

        private void ChangeState(ProsthesisStateBase to)
        {

        }
    }
}

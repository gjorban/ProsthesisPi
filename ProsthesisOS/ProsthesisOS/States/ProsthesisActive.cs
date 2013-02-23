using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class ProsthesisActive : ProsthesisStateBase
    {
        public ProsthesisActive(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            return this;
        }

        public override void OnExit() { }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Pause:
                return new ProsthesisIdle(mContext);
            }
            return base.OnProsthesisCommand(command, from);
        }
    }
}

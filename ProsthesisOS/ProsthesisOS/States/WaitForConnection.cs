using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Messages;
using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class WaitForConnection : ProsthesisStateBase
    {
        public WaitForConnection(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            return this;
        }

        public override void OnExit()
        {

        }

        public override ProsthesisStateBase OnConnection(TCP.ConnectionState state)
        {
            return new AwaitingAuth(mContext);
        }
    }
}

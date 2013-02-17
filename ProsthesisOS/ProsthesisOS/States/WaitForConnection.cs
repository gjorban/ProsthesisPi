using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Messages;

namespace ProsthesisOS.States
{
    internal class WaitForConnection : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            return this;
        }

        public override void OnExit(ProsthesisContext context)
        {

        }

        public override ProsthesisStateBase OnConnection(ProsthesisContext context, TCP.ConnectionState state)
        {
            return new AwaitingAuth();
        }
    }
}

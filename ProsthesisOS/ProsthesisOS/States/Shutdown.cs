using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States
{
    internal class Shutdown : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            context.Terminate("Shutdown");
            return null;
        }

        public override void OnExit(ProsthesisContext context)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States
{
    internal class RunSelfTest : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            return this;
        }

        public override void OnExit(ProsthesisContext context)
        {
            
        }
    }
}

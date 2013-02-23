using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class RunSelfTest : ProsthesisStateBase
    {
        public RunSelfTest(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            return this;
        }

        public override void OnExit()
        {
            
        }
    }
}

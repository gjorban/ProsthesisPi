using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class Shutdown : ProsthesisStateBase
    {
        public Shutdown(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            mContext.Terminate("Shutdown");
            return null;
        }

        public override void OnExit()
        {

        }
    }
}

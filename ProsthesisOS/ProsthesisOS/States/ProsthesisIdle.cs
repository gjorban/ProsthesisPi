using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class ProsthesisIdle : ProsthesisStateBase
    {
        private ArduinoCommunicationsLibrary.ArduinoCommsBase[] mArduinos = null;

        public ProsthesisIdle(IProsthesisContext context, ArduinoCommunicationsLibrary.ArduinoCommsBase[] arduinos) : base(context) 
        {
            mArduinos = arduinos;
        }

        public override ProsthesisStateBase OnEnter()
        {
            foreach (ArduinoCommunicationsLibrary.ArduinoCommsBase arduino in mArduinos)
            {
                arduino.ToggleArduinoState(true);
            }
            return this;
        }

        public override void OnExit() { }

        public override ProsthesisStateBase OnProsthesisCommand(ProsthesisCore.ProsthesisConstants.ProsthesisCommand command, TCP.ConnectionState from)
        {
            switch (command)
            {
            case ProsthesisCore.ProsthesisConstants.ProsthesisCommand.Resume:
                    return new ProsthesisActive(mContext, mArduinos);
            }
            return base.OnProsthesisCommand(command, from);
        }
    }
}

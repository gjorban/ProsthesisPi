using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class WaitForBootup : ProsthesisStateBase
    {
        private ArduinoCommunicationsLibrary.ArduinoCommsBase[] mArduinos = null;

        public WaitForBootup(IProsthesisContext context, ArduinoCommunicationsLibrary.ArduinoCommsBase[] arduinos) : base(context) 
        {
            mArduinos = arduinos;
        }

        public override ProsthesisStateBase OnEnter()
        {
            if (mArduinos != null)
            {
                foreach (ArduinoCommunicationsLibrary.ArduinoCommsBase arduino in mArduinos)
                {
                    if (!arduino.StartArduinoComms())
                    {
                        mContext.RaiseFault(string.Format("Unable to open serial communications with AID {0}", arduino.ArduinoID));
                        return null;
                    }
                    else
                    {
                        arduino.ToggleArduinoState(false);
                        if (!arduino.TelemetryActive)
                        {
                            arduino.TelemetryToggle(100);
                        }
                    }
                }
            }
            return this;
        }

        public override void OnExit()
        {
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

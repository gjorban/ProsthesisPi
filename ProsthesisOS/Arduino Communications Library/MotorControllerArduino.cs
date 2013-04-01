using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore;

namespace ArduinoCommunicationsLibrary
{
    public class MotorControllerArduino : ArduinoCommsBase
    {
        public MotorControllerArduino(ProsthesisCore.Utility.Logger logger) : base("mcon", logger) { }
    }
}

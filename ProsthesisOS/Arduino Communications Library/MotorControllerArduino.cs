﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArduinoCommunicationsLibrary
{
    public sealed class MotorControllerArduino : ArduinoCommsBase
    {
        public const string kArduinoID = "mcon";
        public MotorControllerArduino(ProsthesisCore.Utility.Logger logger) : base(kArduinoID, logger) { }

        protected override void OnTelemetryAvailable(string telemetryData)
        {

        }
    }
}

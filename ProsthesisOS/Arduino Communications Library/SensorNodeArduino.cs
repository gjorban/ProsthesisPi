using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArduinoCommunicationsLibrary
{
    public sealed class SensorNodeArduino : ArduinoCommsBase
    {
        public const string kArduinoID = "sens";
        public SensorNodeArduino(ProsthesisCore.Utility.Logger logger) : base(kArduinoID, logger) { }

        protected override void OnTelemetryAvailable(string telemetryData)
        {

        }
    }
}

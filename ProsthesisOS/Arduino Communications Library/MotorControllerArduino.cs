using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Telemetry;

namespace ArduinoCommunicationsLibrary
{
    public sealed class MotorControllerArduino : ArduinoCommsBase
    {
        public const string kArduinoID = "mcon";
        public MotorControllerArduino(ProsthesisCore.Utility.Logger logger) : base(kArduinoID, logger) { }

        protected override void OnTelemetryReceive(string telemetryData)
        {
            ProsthesisTelemetry.ProthesisMotorTelemetry motorTelem = Newtonsoft.Json.JsonConvert.DeserializeObject<ProsthesisTelemetry.ProthesisMotorTelemetry>(telemetryData);
        }
    }
}

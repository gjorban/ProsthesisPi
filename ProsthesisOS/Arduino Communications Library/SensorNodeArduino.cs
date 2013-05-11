using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Telemetry;

namespace ArduinoCommunicationsLibrary
{
    public sealed class SensorNodeArduino : ArduinoCommsBase
    {
        public event Action<ProsthesisTelemetry.ProsthesisSensorTelemetry> TelemetryUpdate = null;
        public const string kArduinoID = "sens";
        public SensorNodeArduino(ProsthesisCore.Utility.Logger logger) : base(kArduinoID, logger) { }

        protected override void OnTelemetryReceive(string telemetryData)
        {
            try
            {
                ProsthesisTelemetry.ProsthesisMotorTelemetry motorTelem = Newtonsoft.Json.JsonConvert.DeserializeObject<ProsthesisTelemetry.ProsthesisMotorTelemetry>(telemetryData);
                if (TelemetryUpdate != null)
                {
                    TelemetryUpdate(null);
                }
            }
            catch (Exception e)
            {
                if (mLogger != null)
                {
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("AID:{0} failed to parse JSON \"{1}\" because of {2}", mArduinoID, telemetryData, e));
                }
            }
        }
    }
}

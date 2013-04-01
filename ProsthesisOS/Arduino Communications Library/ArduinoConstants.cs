using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArduinoCommunicationsLibrary
{
    /// <summary>
    /// These MUST match the Arduino's values!
    /// </summary>
    public sealed class ArduinoMessageKeys
    {
        public const string kMessageIDKey = "ID";
    }

    public sealed class ArduinoMessageValues
    {
        public const string kAcknowledgeID = "ACK";
        public const string kIdentifyValue = "Id";
        public const string kTelemetryEnableValue = "Te";
        public const string kDeviceToggleValue = "Dt";
        public const string kTelemetryID = "Tm";
        public const string kDeviceStateChange = "Sc";
    }

    public class ArduinoMessageBase
    {
        public string ID;
    }

    public sealed class ArduinoDeviceStateChange : ArduinoMessageBase
    {
        public ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState FR;
        public ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState TO;

        public override string ToString()
        {
            return string.Format("State change from {0} to {1}", FR, TO);
        }
    }

    public sealed class ArduinoToggleResponse : ArduinoMessageBase
    {
        public bool TGR;

        public override string ToString()
        {
            return string.Format("Toggle success: {0}", TGR);
        }
    }

    public sealed class ArduinoIDAckMessage : ArduinoMessageBase
    {
        public string AID;
        public bool TS;
        public ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState DS;

        public override string ToString()
        {
            return string.Format("AID({0}), Telem active: {1}, State: {2}", AID, TS, DS);
        }
    }

    public class ArduinoTelemetryBase : ArduinoMessageBase
    {
        public float x;
        public ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState DS;

        public override string ToString()
        {
            return string.Format("State: {0}, X:{1:0.000}", DS, x);
        }
    }
}

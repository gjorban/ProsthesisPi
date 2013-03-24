using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino_Communications_Test
{
    /// <summary>
    /// These MUST match the Arduino's values!
    /// </summary>
    public sealed class ArduinoConstants
    {
        public const string kMessageIDValue = "ID";
        public const string kToggleResponseID = "ACK";
        public const string kTelemetryID = "TM";
    }

    public class ArduinoMessageBase
    {
        /// <summary>
        /// This enum describes the controller's current state. It MUST match the enum on the Arduino in order to be accurate!
        /// </summary>
        public enum DeviceState
        {
            Uninitialized = -1,
            Disabled = 0,
            Active = 1,
            Fault = 2
        }

        public string ID;
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
        public DeviceState DS;

        public override string ToString()
        {
            return string.Format("AID({0}), Telem active: {1}, State: {2}", AID, TS, DS);
        }
    }

    public class ArduinoTelemetryBase : ArduinoMessageBase
    {
        public float x;
        public DeviceState DS;

        public override string ToString()
        {
            return string.Format("State: {0}, X:{1:0.000}", DS, x);
        }
    }
}

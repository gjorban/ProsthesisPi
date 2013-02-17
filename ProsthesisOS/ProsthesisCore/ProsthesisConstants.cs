using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisCore
{
    public sealed class ProsthesisConstants
    {
        public const string OSVersion = "0.1";
        public const int ConnectionPort = 1337;

        /// <summary>
        /// Enum containing the commands which are sent via the command stream. TBD if this is the right place and correct method of sending commands
        /// </summary>
        public enum ProsthesisCommand
        {
            Initialize,
            Resume,
            Pause,
            Shutdown,
            EmergencyStop
        }
    }
}

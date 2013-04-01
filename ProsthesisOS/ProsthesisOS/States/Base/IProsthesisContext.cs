using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS.States.Base
{
    internal delegate void ProsthesisStateChangeDelegate(ProsthesisStateBase from, ProsthesisStateBase to);

    internal interface IProsthesisContext
    {
        ProsthesisCore.Utility.Logger Logger { get; }
        ProsthesisStateBase CurrentState { get; }
        TCP.TcpServer TCPServer { get; }
        TCP.ConnectionState AuthorizedConnection { get; }

        bool IsRunning { get; }

        void RaiseFault(string description);
        void ChangeState(ProsthesisStateBase to);
        void Terminate(string reason);

        void UpdateMotorTelemetry(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProthesisMotorTelemetry motorTelem);
    }
}

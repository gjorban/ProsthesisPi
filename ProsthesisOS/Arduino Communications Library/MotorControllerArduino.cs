﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Telemetry;

namespace ArduinoCommunicationsLibrary
{
    public sealed class MotorControllerArduino : ArduinoCommsBase
    {
        public event Action<ProsthesisTelemetry.ProthesisMotorTelemetry> TelemetryUpdate = null;
        public const string kArduinoID = "mcon";
        public MotorControllerArduino(ProsthesisCore.Utility.Logger logger) : base(kArduinoID, logger) { }

        private ProsthesisTelemetry.ProthesisMotorTelemetry mCurrentState = null;
        public ProsthesisTelemetry.ProthesisMotorTelemetry DeviceState { get { return mCurrentState; } }

        protected override void OnTelemetryReceive(string telemetryData)
        {
            try
            {
                ProsthesisTelemetry.ProthesisMotorTelemetry motorTelem = Newtonsoft.Json.JsonConvert.DeserializeObject<ProsthesisTelemetry.ProthesisMotorTelemetry>(telemetryData);
                mCurrentState = motorTelem;
                if (TelemetryUpdate != null)
                {
                    TelemetryUpdate(mCurrentState);
                }
            }
            catch (Exception e)
            {
                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("AID:{0} failed to parse JSON \"{1}\" because of {2}", mArduinoID, telemetryData, e));
            }
        }
    }
}
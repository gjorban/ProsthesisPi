using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Ports;

using Newtonsoft.Json;

namespace ArduinoCommunicationsLibrary
{
    public abstract class ArduinoCommsBase
    {
        public event Action<ArduinoMessageBase.DeviceState, ArduinoMessageBase.DeviceState> StateChanged = null;

        protected ProsthesisCore.Utility.Logger mLogger = null;

        protected string mArduinoID = string.Empty;
        public string ArduinoID { get { return mArduinoID; } }

        protected SerialPort mPort = null;
        protected string mPortName = string.Empty;

        protected bool mTelemetryToggled = false;
        protected ArduinoMessageBase.DeviceState mDeviceState = ArduinoMessageBase.DeviceState.Uninitialized;

        protected const int kIDTimeoutMilliseconds = 1000;
        protected const int kArduinoCommsBaudRate = 9600;

        public ArduinoCommsBase(string arduinoID, ProsthesisCore.Utility.Logger logger)
        {
            mArduinoID = arduinoID;
            mLogger = logger;
        }

        public bool StartArduinoComms()
        {
            bool foundCorrectArduino = false;

            var idPacket = new ArduinoMessageBase();
            idPacket.ID = ArduinoMessageValues.kIdentifyValue;

            string jsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(idPacket);
            foreach (string port in SerialPort.GetPortNames())
            {
                SerialPort serialPort = new SerialPort(port);

                //Only check unopened ports
                if (!serialPort.IsOpen)
                {
                    serialPort.BaudRate = kArduinoCommsBaudRate;
                    serialPort.Open();
                    serialPort.DiscardInBuffer(); 
                    serialPort.Write(jsonOutput);
                    serialPort.ReadTimeout = kIDTimeoutMilliseconds;
                    string response = string.Empty;
                    try
                    {
                        response = serialPort.ReadLine();
                    }
                    //Catch case where the serial port is unavailable. MOve to next port
                    catch (TimeoutException)
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Port {0} timed out. Ignoring", port));
                        serialPort.Close();
                        continue;
                    }

                    if (!string.IsNullOrEmpty(response))
                    {
                        try
                        {
                            ArduinoIDAckMessage msg = Newtonsoft.Json.JsonConvert.DeserializeObject < ArduinoIDAckMessage>(response);
                            if (msg.ID == ArduinoMessageValues.kAcknowledgeID && msg.AID == mArduinoID)
                            {
                                mTelemetryToggled = msg.TS;
                                mDeviceState = msg.DS;

                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Got the arduino we're looking for on port {0} with AID {1}. Telemetry is {2} and the device's state is {3}", port, mArduinoID, msg.TS, msg.DS));
                                mPort = serialPort;
                                mPort.DataReceived += new SerialDataReceivedEventHandler(OnSerialDataAvailable);
                                mPort.Disposed += new EventHandler(OnPortDisposed);
                                mPortName = port;
                                foundCorrectArduino = true;
                            }
                            else
                            {
                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Found a Prosthesis Arduino, but not the arduino we're looking for on port {0} with AID {1}", port, mArduinoID));
                            }
                        }
                        //Catch malformed JSON response, if there is one at all
                        catch (Newtonsoft.Json.JsonReaderException)
                        {
                            mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Malformed response. Ignoring port {0}", port));
                        }
                    }
                    else
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Serial port {0} doesn't have an arduino", port));
                    }

                    if (!foundCorrectArduino)
                    {
                        serialPort.Close();
                    }
                }
            }

            return foundCorrectArduino;
        }

        public void StopArduinoComms(bool disableBeforeStop)
        {
            if (mPort != null)
            {
                SerialPort port = mPort;
                mPort = null;
                port.DataReceived -= OnSerialDataAvailable;
                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Closing Arduino comms on port {0} for AID {1}", mPortName, ArduinoID));
                ToggleArduinoState(false);
                port.Close();
            }
        }

        public virtual void TelemetryToggle(int periodMS)
        {
            if (mPort != null && mPort.IsOpen)
            {
                mTelemetryToggled = !mTelemetryToggled;
                var toggle = new { ID = ArduinoMessageValues.kTelemetryEnableValue, EN = mTelemetryToggled, PD = periodMS };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);
                mPort.Write(json);
            }
        }

        public virtual void ToggleArduinoState(bool enable)
        {
            if (mPort != null && mPort.IsOpen)
            {
                var toggle = new { ID = ArduinoMessageValues.kDeviceToggleValue, EN = enable };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);
                mPort.Write(json);
            }
        }

        protected virtual void OnPortDisposed(object sender, EventArgs e)
        {
            if (mPort != null)
            {
                StopArduinoComms(false);
            }
        }

        protected virtual void OnDataAvailable(string data)
        {
            //Log each message from the arduino if we want to see its raw output
            //mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, data);
            Dictionary<string, string> vals = null;
            try
            {
                vals = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            }
            catch
            {
                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Unable to parse JSON message from {0} on port {1}: \"{2}\"", ArduinoID, mPortName, data));
                return;
            }

            string msgId;
            if (vals.TryGetValue(ArduinoMessageKeys.kMessageIDKey, out msgId))
            {
                switch (msgId)
                {
                case ArduinoMessageValues.kTelemetryID:
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("{0}", JsonConvert.DeserializeObject<ArduinoTelemetryBase>(data)));
                    break;
                case ArduinoMessageValues.kAcknowledgeID:
                    //Echo ACKS for now
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("{0}", JsonConvert.DeserializeObject<ArduinoToggleResponse>(data)));
                    break;

                case ArduinoMessageValues.kDeviceStateChange:
                    ArduinoDeviceStateChange changed = JsonConvert.DeserializeObject<ArduinoDeviceStateChange>(data);
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("{0}", changed));
                    if (StateChanged != null)
                    {
                        StateChanged(changed.FR, changed.TO);
                    }
                    mDeviceState = changed.TO;
                    break;
                default:
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Unrecognized JSON message from {0} on port {1}: \"{2}\"", ArduinoID, mPortName, data));
                    break;
                }
            }
        }

        private void OnSerialDataAvailable(object sender, SerialDataReceivedEventArgs e)
        {
            //Pass the data onto the data handler
            OnDataAvailable(mPort.ReadLine());
        }
    }
}

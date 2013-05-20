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
        public event Action<ArduinoCommsBase, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState> StateChanged = null;
        public event Action<ArduinoCommsBase> Disconnected = null;

        protected ProsthesisCore.Utility.Logger mLogger = null;

        protected string mArduinoID = string.Empty;
        public string ArduinoID { get { return mArduinoID; } }

        protected SerialPort mPort = null;
        protected string mPortName = string.Empty;

        public bool IsConnected { get { return mPort != null && mPort.IsOpen; } }

        protected bool mTelemetryToggled = false;
        public bool TelemetryActive { get { return mTelemetryToggled; } }

        public ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState ArduinoState { get { return mDeviceState; } }
        protected ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState mDeviceState = ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Uninitialized;

        private int mMaxMissedDeadlinesBeforeFault = 1;
        private int mNumMissedHeartbeatDeadlines = 0;
        private int mHeartbeatPeriod = 0;

        private System.Timers.Timer mHeartbeatTimer = null;

        protected const int kIDTimeoutMilliseconds = 1000;
        /// <summary>
        /// The amount of time we need to wait for the Arduino bootloader to starts
        /// </summary>
        protected const int kArduinoBootloaderDelayMilliseconds = 3000;
        protected const int kNumRetries = 3;
        protected const int kArduinoCommsBaudRate = 57600;

        private System.Threading.Thread mWorkerThread = null;

        public static bool IsRunningOnLinux()
        {
            int p = (int)Environment.OSVersion.Platform;

            return (p == 4 || p == 128 || p == 6);
        }

        public static void InitializeSerialConnections(ProsthesisCore.Utility.Logger logger)
        {
            string[] ports = GetPortNames();
            logger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Found {0} ports", ports.Length));
            foreach (string port in ports)
            {
                logger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Port {0} found", port));
            }
        }

        public ArduinoCommsBase(string arduinoID, ProsthesisCore.Utility.Logger logger)
        {
            mArduinoID = arduinoID;
            mLogger = logger;
        }

        public bool StartArduinoComms()
        {
            if (mWorkerThread != null)
            {
                mWorkerThread.Abort();
                mWorkerThread = null;
            }

            string[] ports = GetPortNames();
            bool foundCorrectArduino = false;

            var idPacket = new ArduinoMessageBase();
            idPacket.ID = ArduinoMessageValues.kIdentifyValue;

            string jsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(idPacket);

            foreach (string port in ports)
            {
                SerialPort serialPort = new SerialPort(port, kArduinoCommsBaudRate);

                if (mLogger != null)
                {
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Now checking Arduino on port {0}", port));
                }
                //Only check unopened ports
                if (!serialPort.IsOpen)
                {
                    try
                    {
                        serialPort.Open();
                    }
                    //Port is already open in another context, or Arduino. In any case, skip this one
                    catch (UnauthorizedAccessException)
                    {
                        if (mLogger != null)
                        {
                            mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Unable to open port {0} because of unauthorized access exception", port));
                        }
                        continue;
                    }

                    //Wait for the Arduino to boot once we've opened the port
                    System.Threading.Thread.Sleep(kArduinoBootloaderDelayMilliseconds);

                    //Disable telemtry just incase
                    var toggle = new { ID = ArduinoMessageValues.kTelemetryEnableValue, EN = false };
                    string disableTelem = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);
                    serialPort.Write(disableTelem);

                    var toggleHB = new {ID = ArduinoMessageValues.kHeartbeatEnableValue, EN = false };
                    string disableHB = Newtonsoft.Json.JsonConvert.SerializeObject(toggleHB);
                    serialPort.Write(disableHB);

                    //Discard any built up data
                    serialPort.DiscardInBuffer();
                    serialPort.Write(jsonOutput);
                    serialPort.ReadTimeout = kIDTimeoutMilliseconds;

                    string response = string.Empty;
                    for (int i = 0; i < kNumRetries; ++i)
                    {
                        try
                        {
                            response = ReadLine(serialPort);
                            break;
                        }
                        //Catch case where the serial port is unavailable. MOve to next port
                        catch (TimeoutException)
                        {
                            if (mLogger != null)
                            {
                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Port {0} timed out. Attempt {1} of {2}", port, i + 1, kNumRetries));
                            }
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(response))
                    {
                        try
                        {
                            ArduinoIDAckMessage msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ArduinoIDAckMessage>(response);
                            if (msg.ID == ArduinoMessageValues.kAcknowledgeID && msg.AID == mArduinoID)
                            {
                                mTelemetryToggled = msg.TS;
                                mDeviceState = msg.DS;

                                if (mLogger != null)
                                {
                                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Got the arduino we're looking for on port {0} with AID {1}. Telemetry is {2} and the device's state is {3}", port, mArduinoID, msg.TS, msg.DS));
                                }
                                mPort = serialPort;

                                //Don't timeout anymore. Our worker thread will yield while it waits for data
                                mPort.ReadTimeout = -1;
                                mWorkerThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReadSerialDataFromPort));
                                mWorkerThread.Name = string.Format("Arduino IO worker for AID {0}", mArduinoID);

                                mPort.Disposed += new EventHandler(OnPortDisposed);
                                mPortName = port;
                                foundCorrectArduino = true;

                                //Start our worker
                                mWorkerThread.Start();
                            }
                            else if (mLogger != null)
                            {
                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Found a Prosthesis Arduino, but not the arduino we're looking for on port {0} with AID {1}", port, mArduinoID));
                            }
                        }
                        //Catch malformed JSON response, if there is one at all
                        catch (Newtonsoft.Json.JsonSerializationException)
                        {
                            if (mLogger != null)
                            {
                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Malformed response. Ignoring port {0}", port));
                            }
                        }
                        catch (Newtonsoft.Json.JsonReaderException)
                        {
                            if (mLogger != null)
                            {
                                mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Malformed response. Ignoring port {0}", port));
                            }
                        }
                    }
                    else if (mLogger != null)
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Serial port {0} doesn't have an arduino", port));
                    }

                    if (!foundCorrectArduino)
                    {
                        serialPort.Close();
                        serialPort.Dispose();
                    }
                    else
                    {
                        //We've found our Arduino, no need to continue checking
                        break;
                    }
                }
            }

            return foundCorrectArduino;
        }

        public void StopArduinoComms(bool disableBeforeStop)
        {
            if (mDeviceState == ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected)
            {
                return;
            }

            if (mWorkerThread != null)
            {
                mWorkerThread = null;
            }

            if (mHeartbeatTimer != null)
            {
                mHeartbeatTimer.Stop();
            }

            mTelemetryToggled = false;
            mDeviceState = ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected;

            if (mPort != null)
            {
                SerialPort port = mPort;
                if (mLogger != null)
                {
                    mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Closing Arduino comms on port {0} for AID {1}", mPortName, ArduinoID));
                }

                TelemetryToggle(0);
                ToggleArduinoState(false);
                ToggleHeartbeat(false, 0, 0);

                //Set a time out so our thread wakes up and exits
                mPort.ReadTimeout = 1;
                mPort = null;
                try
                {
                    port.Dispose();
                }
                //Eat this exception since we may have had a strange OS disposal
                catch (System.IO.IOException){}
            }

            if (Disconnected != null)
            {
                Disconnected(this);
            }
        }

        /// <summary>
        /// Tells the arduino to start sending telemetry at the given period. NOTE: The arduino may not be capable of sending telemetry at the exact rate specified.
        /// </summary>
        /// <param name="periodMS">The time in milliseconds between telemetry dumps from the Arduino. A value of 0 disables telemetry.</param>
        public virtual void TelemetryToggle(int periodMS)
        {
            if (mPort != null && mPort.IsOpen)
            {
                if (periodMS == 0)
                {
                    mTelemetryToggled = false;
                }
                else
                {
                    mTelemetryToggled = true;
                }
                var toggle = new { ID = ArduinoMessageValues.kTelemetryEnableValue, EN = mTelemetryToggled, PD = periodMS };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);
                //For Arduino MEGAs we don't get proper shut down on windows so this will create an IO exception, stop arduino comms here just incase
                try
                {
                    mPort.Write(json);
                }
                catch (System.IO.IOException)
                {
                    StopArduinoComms(true);
                }
            }
        }

        public virtual void ToggleArduinoState(bool enable)
        {
            if (mPort != null && mPort.IsOpen)
            {
                var toggle = new { ID = ArduinoMessageValues.kDeviceToggleValue, EN = enable };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);

                //For Arduino MEGAs we don't get proper shut down on windows so this will create an IO exception, stop arduino comms here just incase
                try
                {
                    mPort.Write(json);
                }
                catch (System.IO.IOException)
                {
                    StopArduinoComms(true);
                }
            }
        }

        public void ToggleHeartbeat(bool enable, int period, int numMissedDealinesBeforeFault)
        {
            if (mPort != null && mPort.IsOpen)
            {
                var toggle = new { ID = ArduinoMessageValues.kHeartbeatEnableValue, EN = enable, PD = period };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(toggle);

                //For Arduino MEGAs we don't get proper shut down on windows so this will create an IO exception, stop arduino comms here just incase
                try
                {
                    mPort.Write(json);

                    if (enable)
                    {
                        mHeartbeatPeriod = period;
                        mMaxMissedDeadlinesBeforeFault = numMissedDealinesBeforeFault;
                        mNumMissedHeartbeatDeadlines = 0;

                        if (mHeartbeatTimer != null)
                        {
                            mHeartbeatTimer.Stop();
                            mHeartbeatTimer = null;
                        }
                        mHeartbeatTimer = new System.Timers.Timer(mHeartbeatPeriod);
                        mHeartbeatTimer.AutoReset = true;
                        mHeartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(HeartbeatDeadlinePassed);
                    }
                    else if (mHeartbeatTimer != null)
                    {
                        mHeartbeatTimer.Stop();
                        mHeartbeatTimer = null;
                    }

                }
                catch (System.IO.IOException)
                {
                    StopArduinoComms(true);
                }
            }
        }

        protected virtual void OnPortDisposed(object sender, EventArgs e)
        {
            if (mPort != null)
            {
                StopArduinoComms(false);
            }
            //In the case our port was cleared and we got a disposed message, publish the event
            else if (mDeviceState != ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected && Disconnected != null)
            {
                Disconnected(this);
            }
        }

        private void ReadSerialDataFromPort(object context)
        {
            while (mPort != null && mPort.IsOpen)
            {
                string newLine = ReadLine(mPort);
                OnDataAvailable(newLine);
            }
        }

        protected virtual void OnDataAvailable(string data)
        {
            //If we didn't get any data, just return
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            //Log each message from the arduino if we want to see its raw output
            //mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, data);
            var idType = new { ID = "none" };

            idType = JsonConvert.DeserializeAnonymousType(data, idType);

            switch (idType.ID)
            {
                case ArduinoMessageValues.kTelemetryID:
                    OnTelemetryReceive(data);
                    break;
                case ArduinoMessageValues.kAcknowledgeID:
                    //Echo ACKS for now
                    if (mLogger != null)
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("{0}", JsonConvert.DeserializeObject<ArduinoToggleResponse>(data)));
                    }
                    break;

                case ArduinoMessageValues.kDeviceStateChange:
                    ArduinoDeviceStateChange changed = JsonConvert.DeserializeObject<ArduinoDeviceStateChange>(data);
                    if (mLogger != null)
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("{0}", changed));
                    }
                    if (StateChanged != null)
                    {
                        StateChanged(this, changed.FR, changed.TO);
                    }
                    mDeviceState = changed.TO;
                    break;

                case ArduinoMessageValues.kHeartbeatEnableValue:
                    if (mHeartbeatTimer != null)
                    {
                        mHeartbeatTimer.Stop();
                        mHeartbeatTimer.Start();
                        mNumMissedHeartbeatDeadlines = 0;
                    }
                    break;

                default:
                    if (mLogger != null)
                    {
                        mLogger.LogMessage(ProsthesisCore.Utility.Logger.LoggerChannels.Arduino, string.Format("Unrecognized JSON message from {0} on port {1}: \"{2}\"", ArduinoID, mPortName, data));
                    }
                    break;
            }
        }

        protected abstract void OnTelemetryReceive(string telemetryData);

        private void HeartbeatDeadlinePassed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mNumMissedHeartbeatDeadlines++;
            if (mNumMissedHeartbeatDeadlines >= mMaxMissedDeadlinesBeforeFault)
            {
                StopArduinoComms(true);
            }
        }

        /// <summary>
        /// Due to limitations in our Mono environment, only readbyte works. So wrap the readbytes into a readline
        /// See http://www.mono-project.com/HowToSystemIOPorts
        /// </summary>
        /// <param name="onPort"></param>
        /// <returns></returns>
        private string ReadLine(SerialPort onPort)
        {
            if (onPort != null && onPort.IsOpen)
            {
                string returnString = string.Empty;
                try
                {
                    byte tmpByte = (byte)onPort.ReadByte();

                    while (tmpByte != 255 && (char)tmpByte != '\n')
                    {
                        returnString += ((char)tmpByte);
                        tmpByte = (byte)onPort.ReadByte();
                    }

                    return returnString;
                }
                catch (InvalidOperationException)
                {
                    onPort.Dispose();
                    return string.Empty;
                }
                //Catch timeouts induced by the closing of the port
                catch (TimeoutException)
                {
                    onPort.Dispose();
                    return string.Empty;
                }
                //Catch IO exceptions induced by disposing the port on disconnect
                catch (System.IO.IOException)
                {
                    onPort.Dispose();
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// From http://stackoverflow.com/questions/434494/serial-port-rs232-in-mono-for-multiple-platforms
        /// </summary>
        /// <returns></returns>
        private static string[] GetPortNames()
        {
            int p = (int)Environment.OSVersion.Platform;
            List<string> serial_ports = new List<string>();

            // Are we on Unix?
            if (IsRunningOnLinux())
            {
                string[] ttys = System.IO.Directory.GetFiles("/dev/", "tty*");
                foreach (string dev in ttys)
                {
                    //Arduino MEGAs show up as ttyACM due to their different USB<->RS232 chips
                    if (dev.StartsWith("/dev/ttyUSB") || dev.StartsWith("/dev/ttyACM"))
                    {
                        serial_ports.Add(dev);
                    }
                }
            }
            else
            {
                serial_ports.AddRange(SerialPort.GetPortNames());
            }

            return serial_ports.ToArray();
        }
    }
}
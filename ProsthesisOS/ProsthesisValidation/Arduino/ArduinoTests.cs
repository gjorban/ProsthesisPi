using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using ArduinoCommunicationsLibrary;

namespace ProsthesisValidation.Arduino
{
    [TestFixture()]
    public class ArduinoTests
    {
        private ProsthesisCore.Utility.Logger mLogger = null;

        private class InvalidArduino : ArduinoCommsBase
        {
            public InvalidArduino() : base("invalid", null) { }
            protected override void OnTelemetryReceive(string telemetryData)
            {
                throw new NotImplementedException();
            }
        }

        [SetUp()]
        public void Setup()
        {
            //Uncomment for verbose logs
            string fileName = string.Format("{0}-{1}.txt", GetType().Name, System.DateTime.Now.ToString("dd MM yyyy HH-mm-ss"));
           // mLogger = new ProsthesisCore.Utility.Logger(fileName, true);
        }

        [TearDown()]
        public void TearDown()
        {
            if (mLogger != null)
            {
                mLogger.ShutDown();
            }
        }

        [TestCase()]
        public void TestForArduinoConnection()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(mLogger);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsTrue(motorArduino.IsConnected);
            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
            Assert.IsFalse(motorArduino.IsConnected);
        }

        [TestCase()]
        public void TestForInvalidArduinoID()
        {
            ArduinoCommsBase motorArduino = new InvalidArduino();
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsFalse(motorArduino.IsConnected);
            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
        }

        [TestCase()]
        public void TestArduinoStateToggles()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(mLogger);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Uninitialized);
            Assert.DoesNotThrow(connectDel);

            Assert.DoesNotThrow(delegate() { motorArduino.ToggleArduinoState(false); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);

            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disabled);

            Assert.DoesNotThrow(delegate() { motorArduino.ToggleArduinoState(true); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);
            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Active);

            Assert.IsTrue(motorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
            Assert.IsFalse(motorArduino.IsConnected);

            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected);
        }
        
        [TestCase()]
        public void TestArduinoTelemetryToggles()
        {
            MotorControllerArduino motorArduino = new MotorControllerArduino(mLogger);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Uninitialized);
            Assert.DoesNotThrow(connectDel);
            Assert.IsFalse(motorArduino.TelemetryActive);

            Assert.DoesNotThrow(delegate() { motorArduino.ToggleArduinoState(false); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);

            Assert.IsFalse(motorArduino.TelemetryActive);
            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disabled);

            Assert.DoesNotThrow(delegate() { motorArduino.ToggleArduinoState(true); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);
            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Active);

            long telemetryCount = 0;

            motorArduino.TelemetryUpdate += delegate(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry telem)
            {
                telemetryCount++;
            };

            const float cFastestTelemMS = 50;
            int fastestTelem = int.MaxValue;
            //Test at successively smaller telemetry periods. Find out where the arduino falls over, if at all
            for (int i = 1000; i >= cFastestTelemMS; i /= 2)
            {
                fastestTelem = i;
                Console.WriteLine(string.Format("Attemping to receive 10 seconds of telemetry at a period of {0}ms", i));
                telemetryCount = 0;
                Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(i); });
                //Wait 100ms for the message to cycle
                System.Threading.Thread.Sleep(100);
                Assert.IsTrue(motorArduino.TelemetryActive);

                System.Threading.Thread.Sleep(10000);

                Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(0); });
                //Wait 100ms for the message to cycle
                System.Threading.Thread.Sleep(100);
                Assert.IsFalse(motorArduino.TelemetryActive);

                //Telemetry timing isn't a hard realtime component. We'll take receiving 95% of what we asked for as a pass
                bool passed = (float)telemetryCount * 0.95f <= 10.0f * 1000.0f / (float)i;
                if (passed || i > 50)
                {
                    Assert.IsTrue(passed);
                }
                else
                {
                    Console.Write(string.Format("Arduino failed to produce telemetry at {0}ms", i));
                    break;
                }
            }

            //Attempting 30 minutes at fastest allowed telem
            Console.WriteLine(string.Format("Attemping to receive 30 minutes of telemetry at a period of {0}ms with the device enabled", fastestTelem));
            telemetryCount = 0;
            Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(fastestTelem); });
            Assert.DoesNotThrow(delegate() { motorArduino.ToggleArduinoState(true); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);
            Assert.IsTrue(motorArduino.TelemetryActive);

            //30 minute sleep
            Console.WriteLine("\nSystem will now perform a 30 minute soak test to count telemetry packets at the fastest recorded rate. Press ENTER to run this test and any other key to skip it");
            bool doSoak = Console.ReadKey().Key == ConsoleKey.Enter;
            if (doSoak)
            {
                System.Threading.Thread.Sleep(30 * 60 * 1000);

                //Telemetry timing isn't a hard realtime component. We'll take receiving 95% of what we asked for as a pass
                bool passedSoak = (float)telemetryCount * 0.95f <= 1800.0f * 1000.0f / (float)fastestTelem;
                Assert.IsTrue(passedSoak);

                Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(0); });
                //Wait 100ms for the message to cycle
                System.Threading.Thread.Sleep(100);
                Assert.IsFalse(motorArduino.TelemetryActive);

                Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(0); });
                //Wait 100ms for the message to cycle
                System.Threading.Thread.Sleep(100);
                Assert.IsFalse(motorArduino.TelemetryActive);
            }

            Assert.IsTrue(motorArduino.IsConnected);
            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
            Assert.IsFalse(motorArduino.IsConnected);

            Assert.IsFalse(motorArduino.TelemetryActive);
            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected);

            if (fastestTelem > cFastestTelemMS)
            {
                Assert.Inconclusive();
            }
        }

        [TestCase()]
        public void TestArduinoEnumeration()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(mLogger);
            ArduinoCommsBase sensorArduino = new SensorNodeArduino(mLogger);
            TestDelegate sensorConnectDel = new TestDelegate(delegate() { sensorArduino.StartArduinoComms(); });
            TestDelegate motorConnectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(motorConnectDel);
            Assert.DoesNotThrow(sensorConnectDel);

            Assert.IsTrue(sensorArduino.IsConnected);
            Assert.IsTrue(motorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
            Assert.DoesNotThrow(delegate() { sensorArduino.StopArduinoComms(true); });

            Assert.IsFalse(motorArduino.IsConnected);
            Assert.IsFalse(sensorArduino.IsConnected);
        }

        [TestCase()]
        public void TestForMotorControllerDisconnectionWhileInUse()
        {
            Console.WriteLine("Please ensure that the Motor controller ('mcon') is connected for this test. Press ENTER when ready");
            Console.ReadLine();

            ArduinoCommsBase motorArduino = new MotorControllerArduino(mLogger);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsTrue(motorArduino.IsConnected);

            motorArduino.ToggleHeartbeat(true, 500, 3);

            Console.WriteLine(string.Format("Arduino {0} has now been connected. The test will validate behaviour when it is unplugged. Uplug the arduino to continue", motorArduino.ArduinoID));

            System.Threading.ManualResetEvent disconnectEvent = new System.Threading.ManualResetEvent(false);
            motorArduino.Disconnected += delegate(ArduinoCommsBase ard)
            {
                disconnectEvent.Set();
            };

            disconnectEvent.WaitOne();

            Assert.IsFalse(motorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
        }

        [TestCase()]
        public void TestForSensorNodeDisconnectionWhileInUse()
        {
            Console.WriteLine("Please ensure that the sensor arduino ('sens') is connected for this test. Press ENTER when ready");
            Console.ReadLine();

            ArduinoCommsBase sensorArduino = new SensorNodeArduino(mLogger);
            TestDelegate connectDel = new TestDelegate(delegate() { sensorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsTrue(sensorArduino.IsConnected);

            sensorArduino.ToggleHeartbeat(true, 500, 3);

            Console.WriteLine(string.Format("Arduino {0} has now been connected. The test will validate behaviour when it is unplugged. Uplug the arduino to continue", sensorArduino.ArduinoID));

            System.Threading.ManualResetEvent disconnectEvent = new System.Threading.ManualResetEvent(false);
            sensorArduino.Disconnected += delegate(ArduinoCommsBase ard)
            {
                disconnectEvent.Set();
            };

            disconnectEvent.WaitOne();
            Assert.IsFalse(sensorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { sensorArduino.StopArduinoComms(true); });
        }

        [Test()]
        public void TestForEnumeratedDisconnectionWhileInUse()
        {
            Console.WriteLine("Please ensure that both the Motor controller ('mcon') and sensor arduino ('sens') are connected for this test. Press ENTER when ready");
            Console.ReadLine();

            ArduinoCommsBase motorArduino = new MotorControllerArduino(mLogger);
            ArduinoCommsBase sensorArduino = new SensorNodeArduino(mLogger);
            TestDelegate sensorConnectDel = new TestDelegate(delegate() { sensorArduino.StartArduinoComms(); });
            TestDelegate motorConnectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(motorConnectDel);
            Assert.DoesNotThrow(sensorConnectDel);

            Assert.IsTrue(sensorArduino.IsConnected);
            Assert.IsTrue(motorArduino.IsConnected);

            sensorArduino.ToggleHeartbeat(true, 500, 3);
            motorArduino.ToggleHeartbeat(true, 500, 3);

            Console.WriteLine(string.Format("Arduino {0} and {1} has now been connected. The test will validate behaviour when one of them is unplugged. Uplug the arduino to continue", motorArduino.ArduinoID, sensorArduino.ArduinoID));

            System.Threading.ManualResetEvent disconnectEvent = new System.Threading.ManualResetEvent(false);
            motorArduino.Disconnected += delegate(ArduinoCommsBase ard)
            {
                disconnectEvent.Set();
            };

            sensorArduino.Disconnected += delegate(ArduinoCommsBase ard)
            {
                disconnectEvent.Set();
            };

            disconnectEvent.WaitOne();

            Assert.IsFalse(motorArduino.IsConnected && sensorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { sensorArduino.StopArduinoComms(true); });
            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
        }
    }
}

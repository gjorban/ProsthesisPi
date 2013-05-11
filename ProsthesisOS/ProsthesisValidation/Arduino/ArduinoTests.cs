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
        private class InvalidArduino : ArduinoCommsBase
        {
            public InvalidArduino() : base("invalid", null) { }
            protected override void OnTelemetryReceive(string telemetryData)
            {
                throw new NotImplementedException();
            }
        }

        [TestCase()]
        public void TestForArduinoConnection()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(null);
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
            ArduinoCommsBase motorArduino = new MotorControllerArduino(null);
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
            MotorControllerArduino motorArduino = new MotorControllerArduino(null);
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

            int telemetryCount = 0;

            motorArduino.TelemetryUpdate += delegate(ProsthesisCore.Telemetry.ProsthesisTelemetry.ProsthesisMotorTelemetry telem)
            {
                telemetryCount++;
            };

            //Test at successively smaller telemetry periods. Find out where the arduino falls over, if at all
            for (int i = 1000; i >= 5; i /= 2)
            {
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

                //Telemetry timing isn't a hard realtime component. We'll take receiving 75% of what we asked for as a pass
                Assert.LessOrEqual((float)telemetryCount * 0.75f, 10.0f * (float)i);
            }
           
            Assert.DoesNotThrow(delegate() { motorArduino.TelemetryToggle(0); });
            //Wait 100ms for the message to cycle
            System.Threading.Thread.Sleep(100);
            Assert.IsFalse(motorArduino.TelemetryActive);

            Assert.IsTrue(motorArduino.IsConnected);
            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
            Assert.IsFalse(motorArduino.IsConnected);

            Assert.IsFalse(motorArduino.TelemetryActive);
            Assert.AreEqual(motorArduino.ArduinoState, ProsthesisCore.Telemetry.ProsthesisTelemetry.DeviceState.Disconnected);
        }

        [TestCase()]
        public void TestArduinoEnumeration()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(null);
            ArduinoCommsBase sensorArduino = new SensorNodeArduino(null);
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
        public void TestForDisconnectionWhileInUse()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(null);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsTrue(motorArduino.IsConnected);
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
    }
}

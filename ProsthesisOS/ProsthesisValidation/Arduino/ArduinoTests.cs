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

        private class InvalidArduino : ArduinoCommsBase
        {
            public InvalidArduino() : base("invalid", null) { }
            protected override void OnTelemetryReceive(string telemetryData)
            {
                throw new NotImplementedException();
            }
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
        public void TestForDisconnectionWhileInUse()
        {
            ArduinoCommsBase motorArduino = new MotorControllerArduino(null);
            TestDelegate connectDel = new TestDelegate(delegate() { motorArduino.StartArduinoComms(); });

            Assert.DoesNotThrow(connectDel);
            Assert.IsTrue(motorArduino.IsConnected);
            Console.WriteLine("Arduino has now been connected. The test will not continue until it is unplugged");


            Assert.DoesNotThrow(delegate() {
                while (motorArduino.IsConnected)
                {
                    System.Threading.Thread.Sleep(100);
                }
            });

            Assert.IsFalse(motorArduino.IsConnected);

            Assert.DoesNotThrow(delegate() { motorArduino.StopArduinoComms(true); });
        }
    }
}

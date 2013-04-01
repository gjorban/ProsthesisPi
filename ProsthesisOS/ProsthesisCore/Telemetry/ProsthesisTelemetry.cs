using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProtoBuf;

namespace ProsthesisCore.Telemetry
{
    [ProtoContract]
    public sealed class ProsthesisTelemetry : ProsthesisCore.Messages.ProsthesisMessage, ICloneable
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

        [ProtoContract]
        public sealed class ProthesisMotorTelemetry : ICloneable
        {
            [ProtoMember(1)]
            public float[] C = null; //Motor current (Amps)
            [ProtoMember(2)]
            public int[] V = null; //Motor voltage (mV)
            [ProtoMember(3)]
            public float[] Pout = null; //Pressure (kPa) at pump output
            [ProtoMember(4)]
            public float[] Pload = null; //Pressure (kPa) at load
            [ProtoMember(5)]
            public float[] Fl = null; //Flow rate (units TBD)
            [ProtoMember(6)]
            public bool Load = false; //Using load sense or constant pressure
            [ProtoMember(7)]
            public float[] Dt = null; //Motor duty cycle %
            [ProtoMember(8)]
            public DeviceState Ds;

            public ProthesisMotorTelemetry()
            {
                C = new float[0];
                V = new int[0];
                Pout = new float[0];
                Pload = new float[0];
                Fl = new float[0];
                Load = false;
                Dt = new float[0];
                Ds = DeviceState.Uninitialized;
            }

            public ProthesisMotorTelemetry(ProthesisMotorTelemetry other)
            {
                if (other.C != null)
                {
                    Array.Copy(other.C, C, other.C.Length);
                }
                else
                {
                    C = new float[0];
                }

                if (other.V != null)
                {
                    Array.Copy(other.V, V, other.V.Length);
                }
                else
                {
                    V = new int[0];
                }

                if (other.Pout != null)
                {
                    Array.Copy(other.Pout, Pout, other.Pout.Length);
                }
                else
                {
                    Pout = new float[0];
                }

                if (other.Pload != null)
                {
                    Array.Copy(other.Pload, Pload, other.Pload.Length);
                }
                else
                {
                    Pload = new float[0];
                }

                Load = other.Load;

                if (other.Dt != null)
                {
                    Array.Copy(other.Dt, Dt, other.Dt.Length);
                }
                else
                {
                    Dt = new float[0];
                }

                Ds = other.Ds;
            }

            public object Clone()
            {
                return null;
            }
        }

        [ProtoContract]
        public sealed class ProsthesisSensorTelemetry : ICloneable
        {
            //Cotents TBD
            [ProtoMember(1)]
            public DeviceState Ds;

            public ProsthesisSensorTelemetry() 
            {
                Ds = DeviceState.Uninitialized;
            }

            public ProsthesisSensorTelemetry(ProsthesisSensorTelemetry other) 
            {
                Ds = other.Ds;
            }

            public object Clone()
            {
                return new ProsthesisSensorTelemetry(this);
            }
        }

        [ProtoContract]
        public sealed class ProsthesisBMSTelemetry : ICloneable
        {
            //Contents TBD

            public ProsthesisBMSTelemetry() { }

            public ProsthesisBMSTelemetry(ProsthesisBMSTelemetry other) { }

            public object Clone()
            {
                return new ProsthesisBMSTelemetry(this);
            }
        }

        [ProtoMember(1)]
        public ProthesisMotorTelemetry MotorTelem = new ProthesisMotorTelemetry();

        [ProtoMember(2)]
        public ProsthesisSensorTelemetry SensorTelem = new ProsthesisSensorTelemetry();

        [ProtoMember(3)]
        public ProsthesisBMSTelemetry BMSTelem = new ProsthesisBMSTelemetry();

        public ProsthesisTelemetry() { }
        public ProsthesisTelemetry(ProsthesisTelemetry other)
        {
            MotorTelem = new ProthesisMotorTelemetry(other.MotorTelem);
            SensorTelem = new ProsthesisSensorTelemetry(other.SensorTelem);
            BMSTelem = new ProsthesisBMSTelemetry(other.BMSTelem);
        }
        
        public object Clone()
        {
            return new ProsthesisTelemetry(this);
        }
    }
}

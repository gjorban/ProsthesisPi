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

        /// <summary>
        /// Note, this is a Proto-buf and JSON adapter class! The member names MUST match those in the respective telemetry JSON packets being sent by the Arduinos!!
        /// </summary>
        [ProtoContract]
        public sealed class ProthesisMotorTelemetry : ICloneable
        {
            /// <summary>
            /// Motor current (Amps)
            /// </summary>
            [ProtoMember(1)]
            public float[] C = null;
            /// <summary>
            /// Motor voltage (mV)
            /// </summary>
            [ProtoMember(2)]
            public int[] V = null;
            /// <summary>
            /// Pressure (kPa) at pump output
            /// </summary>
            [ProtoMember(3)]
            public float[] Pout = null;
            /// <summary>
            /// Pressure (kPa) at load
            /// </summary>
            [ProtoMember(4)]
            public float[] Pload = null;
            /// <summary>
            /// Flow rate (units TBD)
            /// </summary>
            [ProtoMember(5)]
            public float[] Fl = null;
            /// <summary>
            /// Using load sense or constant pressure
            /// </summary>
            [ProtoMember(6)]
            public bool Load = false;
            /// <summary>
            /// Motor duty cycle %
            /// </summary>
            [ProtoMember(7)]
            public float[] Dt = null;
            /// <summary>
            /// Device state
            /// </summary>
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

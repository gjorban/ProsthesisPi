using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

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
            Disconnected = -2,
            Uninitialized = -1,
            Disabled = 0,
            Active = 1,
            Fault = 2
        }

        /// <summary>
        /// Note, this is a Proto-buf and JSON adapter class! The member names MUST match those in the respective telemetry JSON packets being sent by the Arduinos!!
        /// </summary>
        [ProtoContract]
        [System.Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class ProsthesisMotorTelemetry : ICloneable
        {
            private const int kDefaultNumMotors = 2;

            /// <summary>
            /// Motor current (Amps)
            /// </summary>
            [ProtoMember(1)]
            public float[] C = null;
            public float[] Current { get { return C; } set { C = value; } }
            /// <summary>
            /// Motor voltage (mV)
            /// </summary>
            [ProtoMember(2)]
            public int[] V = null;
            public int[] MilliVolts { get { return V; } set { V = value; } }

            /// <summary>
            /// Pressure (kPa) at pump output
            /// </summary>
            [ProtoMember(3)]
            public float[] Pout = null;
            public float[] OutputPressure { get { return Pout; } set { Pout = value; } }

            /// <summary>
            /// Pressure (kPa) at load
            /// </summary>
            [ProtoMember(4)]
            public float[] Pload = null;
            public float[] PressureLoad { get { return Pload; } set { Pload = value; } }

            /// <summary>
            /// Flow rate (units TBD)
            /// </summary>
            [ProtoMember(5)]
            public float[] Fl = null;
            public float[] FlowRate { get { return Fl; } set { Fl = value; } }

            /// <summary>
            /// Using load sense or constant pressure
            /// </summary>
            [ProtoMember(6)]
            public bool Load = false;
            public bool UsingLoadSense { get { return Load; } set { Load = value; } }

            /// <summary>
            /// Motor duty cycle %
            /// </summary>
            [ProtoMember(7)]
            public float[] Dt = null;
            public float[] MotorDutyCycles { get { return Dt; } set { Dt = value; } }

            /// <summary>
            /// Device state
            /// </summary>
            [ProtoMember(8)]
            public DeviceState Ds;
            public DeviceState DeviceState { get { return Ds; } set { Ds = value; } }

            //Pressure set points in kPa
            [ProtoMember(9)]
            public float[] Pset = null;
            public float[] PressureSetPoints { get { return Pset; } set { Pset = value; } }

            public float LeftEfficiency
            {
                //TODO: Correct calculations?
                get { return (Fl[0] * Pout[0]) / (C[0] * V[0]); }
            }

            public float RightEfficiency
            {
                //TODO: Correct calculations?
                get { return (Fl[1] * Pout[1]) / (C[1] * V[1]); }
            }

            public ProsthesisMotorTelemetry()
            {
                C = new float[0];
                V = new int[0];
                Pout = new float[0];
                Pload = new float[0];
                Fl = new float[0];
                Load = false;
                Dt = new float[0];
                Ds = DeviceState.Disconnected;
                Pset = new float[0];
            }

            public ProsthesisMotorTelemetry(ProsthesisMotorTelemetry other)
            {
                if (other.C != null)
                {
                    C = new float[other.C.Length];
                    Array.Copy(other.C, C, other.C.Length);
                }
                else
                {
                    C = new float[0];
                }

                if (other.V != null)
                {
                    V = new int[other.V.Length];
                    Array.Copy(other.V, V, other.V.Length);
                }
                else
                {
                    V = new int[0];
                }

                if (other.Pout != null)
                {
                    Pout = new float[other.Pout.Length];
                    Array.Copy(other.Pout, Pout, other.Pout.Length);
                }
                else
                {
                    Pout = new float[0];
                }

                if (other.Pload != null)
                {
                    Pload = new float[other.Pload.Length];
                    Array.Copy(other.Pload, Pload, other.Pload.Length);
                }
                else
                {
                    Pload = new float[0];
                }

                Load = other.Load;

                if (other.Dt != null)
                {
                    Dt = new float[other.Dt.Length];
                    Array.Copy(other.Dt, Dt, other.Dt.Length);
                }
                else
                {
                    Dt = new float[0];
                }

                Ds = other.Ds;

                if (other.Pset != null)
                {
                    Pset = new float[other.Pset.Length];
                    Array.Copy(other.Pset, Pset, Pset.Length);
                }
                else
                {
                    Pset = new float[0];
                }
            }

            public object Clone()
            {
                return new ProsthesisMotorTelemetry(this);
            }
        }

        [ProtoContract]
        [System.Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class ProsthesisSensorTelemetry : ICloneable
        {
            //Cotents TBD
            [ProtoMember(1)]
            public DeviceState Ds;

            /// <summary>
            /// Oil temperatures in Celsius
            /// </summary>
            [ProtoMember(2)]
            public float[] Ot;
            public float[] OilTemps { get { return Ot; } set { Ot = value; } }

            /// <summary>
            /// Motor temperatures in Celsius
            /// </summary>
            [ProtoMember(3)]
            public float[] Mt;
            public float[] MotorTemps { get { return Mt; } set { Mt = value; } }
            

            public ProsthesisSensorTelemetry() 
            {
                Ds = DeviceState.Uninitialized;

                Ot = new float[0];
                Mt = new float[0];
            }

            public ProsthesisSensorTelemetry(ProsthesisSensorTelemetry other) 
            {
                Ds = other.Ds;

                if (other.Ot != null)
                {
                    Ot = new float[other.Ot.Length];
                    Array.Copy(other.Ot, Ot, Ot.Length);
                }
                else
                {
                    Ot = new float[0];
                }

                if (other.Mt != null)
                {
                    Mt = new float[other.Mt.Length];
                    Array.Copy(other.Mt, Mt, Mt.Length);
                }
                else
                {
                    Mt = new float[0];
                }
            }

            public object Clone()
            {
                return new ProsthesisSensorTelemetry(this);
            }
        }

        [ProtoContract]
        [System.Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class ProsthesisBMSTelemetry : ICloneable
        {
            //Contents TBD
            [ProtoMember(1)]
            public DeviceState Ds;

            public ProsthesisBMSTelemetry() { }

            public ProsthesisBMSTelemetry(ProsthesisBMSTelemetry other) 
            {
                Ds = other.Ds;
            }

            public object Clone()
            {
                return new ProsthesisBMSTelemetry(this);
            }
        }

        [ProtoMember(1)]
        public ProsthesisMotorTelemetry MT = new ProsthesisMotorTelemetry();
        public ProsthesisMotorTelemetry MotorTelem { get { return MT; } set { MT = value; } }

        [ProtoMember(2)]
        public ProsthesisSensorTelemetry ST = new ProsthesisSensorTelemetry();
        public ProsthesisSensorTelemetry SensorTelem { get { return ST; } set { ST = value; } }

        [ProtoMember(3)]
        public ProsthesisBMSTelemetry BMST = new ProsthesisBMSTelemetry();
        public ProsthesisBMSTelemetry BMSTelem { get { return BMST; } set { BMST = value; } }

        public ProsthesisTelemetry() { }
        public ProsthesisTelemetry(ProsthesisTelemetry other)
        {
            MT = new ProsthesisMotorTelemetry(other.MT);
            ST = new ProsthesisSensorTelemetry(other.ST);
            BMST = new ProsthesisBMSTelemetry(other.BMST);
        }
        
        public object Clone()
        {
            return new ProsthesisTelemetry(this);
        }
    }
}

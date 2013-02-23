using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;

namespace ProsthesisCore.Messages
{
    [ProtoInclude(1, typeof(ProsthesisHandshakeRequest))]
    [ProtoInclude(2, typeof(ProsthesisHandshakeResponse))]
    [ProtoInclude(3, typeof(ProsthesisCommand))]
    [ProtoInclude(4, typeof(ProsthesisCommandAck))]
    [ProtoInclude(5, typeof(ProsthesisTelemetryContainer))]
    [ProtoContract]
    public abstract class ProsthesisMessage { }

    [ProtoContract]
    public class ProsthesisHandshakeRequest : ProsthesisMessage
    {
        [ProtoMember(1)]
        public string VersionId;
    }

    [ProtoContract]
    public class ProsthesisHandshakeResponse : ProsthesisMessage
    {
        public const int kBadPort = -1;

        [ProtoMember(1)]
        public int ControlPort;
        [ProtoMember(2)]
        public bool AuthorizedConnection;
        [ProtoMember(3)]
        public string ErrorString;
    }

    [ProtoContract]
    public class ProsthesisCommand : ProsthesisMessage
    {
        [ProtoMember(1)]
        public ProsthesisConstants.ProsthesisCommand Command;
    }

    [ProtoContract]
    public class ProsthesisCommandAck : ProsthesisMessage
    {
        [ProtoMember(1)]
        public ProsthesisConstants.ProsthesisCommand Command;
        [ProtoMember(2)]
        public long Timestamp;
    }

    [ProtoContract]
    public class ProsthesisTelemetryContainer : ProsthesisMessage, ICloneable
    {
        public ProsthesisTelemetryContainer() 
        {
            StateName = string.Empty;
            MachineActive = false;
            HydraulicPressure = 0f;
            if (MotorDutyCycles == null)
            {
                MotorDutyCycles = new float[0];
            }

            for (int i = 0; i < MotorDutyCycles.Length; ++i)
            {
                MotorDutyCycles[i] = 0f;
            }

            if (CellVoltages == null)
            {
                CellVoltages = new float[0];
            }

            for (int i = 0; i < CellVoltages.Length; ++i)
            {
                CellVoltages[i] = 0f;
            }

            HydraulicPressure = 0f;
        }

        public ProsthesisTelemetryContainer(int numMotors, int numCells)
            : base()
        {
            MotorDutyCycles = new float[numMotors];
            CellVoltages = new float[numCells];
        }

        public ProsthesisTelemetryContainer(ProsthesisTelemetryContainer other)
        {
            StateName = other.StateName;
            MachineActive = other.MachineActive;
            HydraulicPressure = other.HydraulicPressure;

            MotorDutyCycles = new float[other.MotorDutyCycles.Length];
            Array.Copy(other.MotorDutyCycles, MotorDutyCycles, other.MotorDutyCycles.Length);

            CellVoltages = new float[other.CellVoltages.Length];
            Array.Copy(other.CellVoltages, CellVoltages, other.CellVoltages.Length);
            HydraulicTemperature = other.HydraulicTemperature;
        }

        public object Clone() { return new ProsthesisTelemetryContainer(this); }

        [ProtoMember(1)]
        public string StateName;
        [ProtoMember(2)]
        public bool MachineActive;
        [ProtoMember(3)]
        public float HydraulicPressure;
        [ProtoMember(4)]
        public float[] MotorDutyCycles;
        [ProtoMember(5)]
        public float[] CellVoltages;
        [ProtoMember(6)]
        public float HydraulicTemperature;

        public override string ToString()
        {
            string motorStrings = string.Empty;
            string cellVoltageString = string.Empty;

            if (MotorDutyCycles != null)
            {
                for (int i = 0; i < MotorDutyCycles.Length; ++i)
                {
                    motorStrings += string.Format("Motor {0} duty cycle: {1:0.00}%", i, MotorDutyCycles[i]);
                    if (i < MotorDutyCycles.Length - 1)
                    {
                        motorStrings += "\n";
                    }
                }
            }
            else
            {
                motorStrings = "No motor data available";
            }

            if (CellVoltages != null)
            {
                for (int i = 0; i < CellVoltages.Length; ++i)
                {
                    cellVoltageString += string.Format("Cell {0} voltage: {1}V", i, CellVoltages[i]);
                    if (i < CellVoltages.Length - 1)
                    {
                        cellVoltageString += "\n";
                    }
                }
            }
            else
            {
                cellVoltageString = "No cell data available";
            }

            string state = string.Format("State Name: {0}\nMachine Active: {1}\nHydraulic Pressure (kPA): {2}\n{3}\n{4}\nHydraulic Temperature: {5}", 
                StateName, 
                MachineActive ? "yes" : "no",
                HydraulicPressure,
                motorStrings,
                cellVoltageString,
                HydraulicTemperature);

            return state;
        }
    }

    [ProtoContract]
    public class ProsthesisDataPacket
    {
        public static ProsthesisDataPacket BoxMessage<T>(T message) where T : ProsthesisMessage
        {
            try
            {
                System.IO.MemoryStream outBuff = new System.IO.MemoryStream();
                ProtoBuf.Serializer.SerializeWithLengthPrefix<T>(outBuff, message, ProtoBuf.PrefixStyle.Fixed32);

                int dataSize = (int)outBuff.Position;
                outBuff.Position = 0;

                return new ProsthesisDataPacket(outBuff.ToArray(), dataSize);
            }
            catch
            {
                return null;
            }
        }

        public static ProsthesisMessage UnboxMessage(ProsthesisDataPacket packet)
        {
            try
            {
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(packet.Data);
                memStream.Position = 0;
                ProsthesisMessage mess = ProtoBuf.Serializer.DeserializeWithLengthPrefix<ProsthesisMessage>(memStream, PrefixStyle.Fixed32);
                return mess;
            }
            catch
            {
                return null;
            }
        }

        public static int HeaderSize
        {
            //Packet header + packet size
            get { return sizeof(uint) + sizeof(int); }
        }

        public static int FooterSize
        {
            get { return sizeof(uint); }
        }

        public ProsthesisDataPacket(byte[] data, int length)
        {
            PacketLength = length;
            Data = new byte[length];
            Array.Copy(data, 0, Data, 0, length);
        }

        public byte[] Bytes
        {
            get
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(PacketStart));
                bytes.AddRange(BitConverter.GetBytes(PacketLength));
                bytes.AddRange(Data);
                bytes.AddRange(BitConverter.GetBytes(PacketEnd));

                return bytes.ToArray();
            }
        }

        public const uint kPacketStart = 0xDEADBEEF;
        public const uint kPacketEnd = 0xBEEFEBEF;
        [ProtoMember(1)]
        public readonly uint PacketStart = kPacketStart;
        [ProtoMember(2)]
        public int PacketLength;
        [ProtoMember(3)]
        public byte[] Data;
        [ProtoMember(4)]
        public readonly uint PacketEnd = kPacketEnd;
    }
}
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace ProsthesisCore.Messages
{
    [ProtoInclude(1,typeof(ProsthesisHandshakeRequest))]
    [ProtoInclude(2, typeof(ProsthesisHandshakeResponse))]
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisCore
{
    public sealed class ProsthesisPacketParser : IEnumerator<Messages.ProsthesisMessage>
    {
        private List<byte> mMemBuffer = new List<byte>(2056);

        private Messages.ProsthesisDataPacket mCurrentDataPacket = null;
        private Messages.ProsthesisMessage mCurrentMessage = null;

        public Messages.ProsthesisDataPacket RawPacket { get { return mCurrentDataPacket; } }
        object System.Collections.IEnumerator.Current { get { return mCurrentMessage; } }
        public Messages.ProsthesisMessage Current { get { return mCurrentMessage; } }

        public void AddData(byte[] data, int length)
        {
            lock (this)
            {
                for (int i = 0; i < length && i < data.Length; ++i)
                {
                    mMemBuffer.Add(data[i]);
                }
            }
        }

        public bool MoveNext()
        {
            bool hasFullPacket = false;
            int headerSize = ProsthesisCore.Messages.ProsthesisDataPacket.HeaderSize;
            int footerSize = ProsthesisCore.Messages.ProsthesisDataPacket.FooterSize;
            lock (this)
            {
                byte[] memBufferArray = mMemBuffer.ToArray();
                //We want at the very least, a packet begin and size descriptor for the binary data
                if (mMemBuffer.Count >= headerSize)
                {
                    uint packetStart = BitConverter.ToUInt32(memBufferArray, 0);
                    if (packetStart == Messages.ProsthesisDataPacket.kPacketStart)
                    {
                        int sizeOffset = sizeof(uint);
                        int packetSize = BitConverter.ToInt32(memBufferArray, sizeOffset);
                        //Check to see if we have the full packet
                        if (mMemBuffer.Count >= headerSize + packetSize + footerSize)
                        {
                            //Verify that the footer is the correct type
                            uint packetEnd = BitConverter.ToUInt32(memBufferArray, headerSize + (int)packetSize);
                            if (packetEnd == Messages.ProsthesisDataPacket.kPacketEnd)
                            {
                                hasFullPacket = true;
                                //Decode the packet!
                                System.IO.MemoryStream memStream = new System.IO.MemoryStream(memBufferArray, headerSize, (int)packetSize);

                                mCurrentDataPacket = new Messages.ProsthesisDataPacket(memStream.ToArray(), (int)memStream.Length);
                                mCurrentMessage = ProsthesisCore.Messages.ProsthesisDataPacket.UnboxMessage(mCurrentDataPacket);

                                mMemBuffer.RemoveRange(0, headerSize + (int)packetSize + footerSize);
                            }
                        }
                    }
                }
            }
            return hasFullPacket;
        }

        public void Reset()
        {
            mMemBuffer.Clear();
        }

        public void Dispose()
        {

        }

    }
}

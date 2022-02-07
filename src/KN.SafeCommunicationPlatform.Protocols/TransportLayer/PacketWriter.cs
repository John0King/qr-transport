using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Buffers;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class PacketWriter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="destination">应为1040字节 </param>
        /// <returns>byte write</returns>
        public int Write(Packet packet,Span<byte> destination)
        {
            int byteWrite = 0;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(0, 4), packet.SessionId);
            byteWrite += 4;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), packet.PacketId);
            byteWrite += 4;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), (uint)packet.OpCode);
            byteWrite += 4;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), packet.PacketSize);
            byteWrite += 4;
            packet.Payload.CopyTo(destination.Slice(0, 4));
            byteWrite += (int)packet.PacketSize;
            return byteWrite;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    [StructLayout(LayoutKind.Sequential,Pack = 1,Size = 1040)]
    public struct Packet
    {
        public uint SessionId;
        public uint PacketId;
        [MarshalAs(UnmanagedType.U4)]
        public OpCode OpCode;
        public uint PacketSize;
        public byte[] Payload;
    }

    public enum OpCode : uint
    {
        Send = 0x01,
        SendLast = 0x02,
        Ack = 0x10,
        Repeat = 0x11,
        Ping = 0x20,
        Pong = 0x21,
    }
}

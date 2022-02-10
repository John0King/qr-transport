using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    [StructLayout(LayoutKind.Sequential,Pack = 1,Size = 1000)]
    public struct Packet
    {
        public uint SessionId;
        public uint PacketId;
        [MarshalAs(UnmanagedType.U2)]
        public OpCode OpCode;
        [MarshalAs(UnmanagedType.U1)]
        public bool EndOfMessage;
        public uint PacketSize;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 984)]
        public byte[] Payload;
    }

    public enum OpCode : ushort
    {
        Send = 0x01,
        Close = 0x02,
        Ack = 0x10,
        Repeat = 0x11,
        Ping = 0x20,
        Pong = 0x21,
    }
}

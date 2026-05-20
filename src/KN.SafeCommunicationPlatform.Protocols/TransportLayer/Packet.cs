using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1000)]
    public struct Packet
    {
        [FieldOffset(0)]
        public uint SessionId;
        [FieldOffset(4)]
        public uint PacketId;
        [FieldOffset(8)]
        [MarshalAs(UnmanagedType.U2)]
        public OpCode OpCode;
        [FieldOffset(10)]
        [MarshalAs(UnmanagedType.U1)]
        public bool EndOfMessage;
        [FieldOffset(12)]
        public uint PacketSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 984)]
        [FieldOffset(16)]
        public byte[] Payload;

        public static int MaxPayload => 984;
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

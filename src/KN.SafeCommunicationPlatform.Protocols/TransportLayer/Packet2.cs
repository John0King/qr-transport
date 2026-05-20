using System.Runtime.InteropServices;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1000)]
    public struct Packet2
    {
        [FieldOffset(0)]
        public uint SessionId;

        [FieldOffset(4)]
        public uint PacketId;

        [FieldOffset(8)]
        [MarshalAs(UnmanagedType.U2)]
        public CtrlCode CtrlCode;

        [FieldOffset(10)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort PacketSize;

        [FieldOffset(12)]
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeConst = 988)]
        private byte _playload;

        public Span<byte> Playload
        {
            get
            {
                return MemoryMarshal.CreateSpan(ref this._playload, 988);
            }
            set
            {
                var s = MemoryMarshal.CreateSpan(ref this._playload, 988);
                value.CopyTo(s);
            }
        }
    }
}

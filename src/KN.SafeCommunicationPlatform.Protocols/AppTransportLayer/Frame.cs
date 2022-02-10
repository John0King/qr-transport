using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace KN.SafeCommunicationPlatform.Protocols.AppTransportLayer
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 2048)]
    public struct FileFrame
    {
        public readonly byte Mark = 0xFA;
        public FrameOp OpCode;
        public ulong ContentLength;
        public uint ContentIndex;
        public uint ContentIndxCount;

        /// <summary>
        /// Sha1 Hash
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Hash = default!;
        public byte[] Data = default!;
    }
}

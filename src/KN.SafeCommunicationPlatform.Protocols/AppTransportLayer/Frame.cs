using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace KN.SafeCommunicationPlatform.Protocols.AppTransportLayer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 2048)]
    public struct FileFrame
    {
        public FileFrame()
        {

        }
        public readonly byte Mark = 0xFA;
        public FrameOp OpCode = FrameOp.New;
        public ulong ContentLength = 0;
        public uint ContentIndex = 0;
        public uint ContentIndxCount = 0;

        /// <summary>
        /// Sha1 Hash
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Hash = default!;
        public byte[] Data = default!;
    }
}

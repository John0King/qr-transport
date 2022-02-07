using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.AppTransportLayer
{
    public struct Frame
    {
        public readonly byte Mark = 0xFA;
        public uint OpCode;
        public ulong ContentLength;
        public byte[] Data;
    }
}

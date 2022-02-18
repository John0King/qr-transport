using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Buffers;
using KN.SafeCommunicationPlatform.Protocols.Qr;
using System.Runtime.InteropServices;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class PacketWriter:IDisposable
    {
        private IQrWriter qrWriter;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);

        public PacketWriter(IQrWriter qrWriter)
        {
            this.qrWriter = qrWriter;
            var size = Marshal.SizeOf<Packet>();
            MemoryOwner = MemoryPool<byte>.Shared.Rent(size);
        }

        public IMemoryOwner<byte> MemoryOwner { get; }

        public void Dispose()
        {
            MemoryOwner.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="destination">应为1040字节 </param>
        /// <returns>byte write</returns>
        private int Write(ref Packet packet,Span<byte> destination)
        {
            int byteWrite = 0;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(0, 4), packet.SessionId);
            byteWrite += 4;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), packet.PacketId);
            byteWrite += 4;
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(8, 2), (ushort)packet.OpCode);
            byteWrite += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(10, 2), Convert.ToUInt16(packet.EndOfMessage));
            byteWrite += 2;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), packet.PacketSize);
            byteWrite += 4;
            packet.Payload.CopyTo(destination.Slice(byteWrite, (int)packet.PacketSize));
            byteWrite += (int)packet.PacketSize;
            return byteWrite;
        }

        public async ValueTask WritePacket(Packet packet)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                var size = Write(ref packet, MemoryOwner.Memory.Span);
                qrWriter.WriteQrData(MemoryOwner.Memory.Span.Slice(0, size));
                await Task.Delay(200);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            
        }
    }
}

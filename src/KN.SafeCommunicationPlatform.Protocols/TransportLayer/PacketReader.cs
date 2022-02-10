using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class PacketReader
    {
        private readonly IQrReader _qrReader;

        public PacketReader(IQrReader qrReader)
        {
            _qrReader = qrReader;
        }

        private Packet Read(ReadOnlySpan<byte> packetStream,out int byteRead)
        {
            var packet = new Packet();
            packet.SessionId = BinaryPrimitives.ReadUInt32BigEndian(packetStream.Slice(0, 4));
            packet.PacketId = BinaryPrimitives.ReadUInt32BigEndian(packetStream.Slice(4, 4));
            packet.OpCode = (OpCode)BinaryPrimitives.ReadUInt16BigEndian(packetStream.Slice(8, 2));
            packet.EndOfMessage = Convert.ToBoolean(BinaryPrimitives.ReadUInt16LittleEndian(packetStream.Slice(10,2)));
            packet.PacketSize = BinaryPrimitives.ReadUInt32BigEndian(packetStream.Slice(12, 4));
            packet.Payload = packetStream.Slice(16,(int)packet.PacketSize).ToArray();

            byteRead = 4 + 4 + 2 + 2 + 4 + (int)packet.PacketSize;
            return packet;
        }

        
        public async IAsyncEnumerable<Packet> GetPacketStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _qrReader.StartRead();
            await foreach(var mem in _qrReader.GetQrStream(cancellationToken))
            {
                var packet = Read(mem.Span, out _);
                yield return packet;
            }
        }
    }
}

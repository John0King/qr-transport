using KN.SafeCommunicationPlatform.Protocols.Internal;
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
        private readonly LossSet _remoteId = new LossSet(3);
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
            cancellationToken.Register(()=> _qrReader.StopRead());
            await foreach(var mem in _qrReader.GetQrStream(cancellationToken))
            {
                Packet packet;
                try
                {
                    packet = Read(mem.Span, out _);
                }
                catch(Exception ex)
                {
                    throw new InvalidDataException("Packet Can not be deserialzed",ex);
                }
                if (!_remoteId.Has(packet.PacketId))
                {
                    _remoteId.Put(packet.PacketId);
                    yield return packet;
                }
                await Task.Delay(10);
            }
        }
    }
}

using KN.SafeCommunicationPlatform.Protocols.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class PacketTransportHandler
    {
        private readonly PacketReader _reader;
        private readonly PacketWriter _writer;
        private readonly LossSet _cursor = new LossSet(4);

        public PacketTransportHandler(PacketReader reader, PacketWriter writer)
        {
            this._reader = reader;
            this._writer = writer;
        }

        public async ValueTask SendData(Memory<byte> buffer)
        {
            var pos = 0;
            while(pos < buffer.Length)
            {
                var size = Math.Min(buffer.Length, Packet.MaxPayload);
                var p = new Packet
                {
                    SessionId = 0,
                    PacketId = _cursor.PutNext(),
                    OpCode = OpCode.Send,
                    EndOfMessage = pos + size >= buffer.Length,
                    PacketSize = (uint)size,
                    Payload = buffer.Slice(pos, size).ToArray()
                };
                await SendDataCore(p);
                pos += size;
            }
            

        }
        
        public async ValueTask SendDataCore(Packet packet)
        {
            _writer.WritePacket(ref packet);
            var tor = _reader.GetPacketStream().GetAsyncEnumerator();
            await tor.MoveNextAsync();
            var respose = tor.Current;
            if(respose.OpCode == OpCode.Ack)
            {
                return;
            }
            throw new InvalidDataException($"Wait for {OpCode.Ack},but get {respose.OpCode}");
        }

        public async ValueTask SendPing(Packet packet)
        {
            _writer.WritePacket(ref packet);
            var tor = _reader.GetPacketStream().GetAsyncEnumerator();
            await tor.MoveNextAsync();
            var respose = tor.Current;
            if (respose.OpCode == OpCode.Ack)
            {
                return;
            }
            throw new InvalidDataException($"Wait for {OpCode.Pong},but get {respose.OpCode}");
        }

        

        public async ValueTask SendPong(Packet packet)
        {
            _writer.WritePacket(ref packet);
            await Task.Delay(500);
        }


        public async ValueTask<Memory<byte>> ReceiveAsync()
        {
            var tor = _reader.GetPacketStream().GetAsyncEnumerator();
            await tor.MoveNextAsync();
            var respose = tor.Current;
            throw new NotImplementedException();
        }
    }
}

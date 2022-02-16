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
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0,1);
        private readonly byte[] Empty = Array.Empty<byte>();

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
                if(respose.PacketId != _cursor.GetLast())
                {
                    Console.WriteLine($"Error, ack {respose.PacketId} is not for send{_cursor.GetLast()}");
                }
                return;
            }
            throw new InvalidDataException($"Wait for {OpCode.Ack},but get {respose.OpCode}");
        }

        public async ValueTask SendPing(Packet packet)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                _writer.WritePacket(ref packet);
                var tor = _reader.GetPacketStream().GetAsyncEnumerator();
                await tor.MoveNextAsync();
                await tor.DisposeAsync();
                var respose = tor.Current;
                if (respose.OpCode == OpCode.Ack)
                {
                    return;
                }
                throw new InvalidDataException($"Wait for {OpCode.Pong},but get {respose.OpCode}");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        

        public async ValueTask SendPong(Packet packet)
        {
            _writer.WritePacket(ref packet);
            await Task.Delay(500);
        }


        public async ValueTask<QrReceiveResult> ReceiveAsync(Memory<byte> buffer)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                var tor = _reader.GetPacketStream().GetAsyncEnumerator();
                await tor.MoveNextAsync();
                var respose = tor.Current;
                _cursor.Put(respose.PacketId);
                await tor.DisposeAsync();
                if (respose.OpCode == OpCode.Send)
                {
                    respose.Payload.AsMemory().CopyTo(buffer);
                    var ack = new Packet
                    {
                        SessionId = 0,
                        PacketId = _cursor.GetLast(),
                        PacketSize = 0,
                        EndOfMessage = true,
                        OpCode = OpCode.Ack,
                        Payload = this.Empty
                    };
                    _writer.WritePacket(ref ack);
                    await Task.Delay(500);//等待对方确认
                    return new QrReceiveResult((int)respose.PacketSize, MessageType.Binary, respose.EndOfMessage);
                }
                else if(respose.OpCode == OpCode.Close)
                {
                    return new QrReceiveResult(0, MessageType.Close, true);
                }
                else
                {
                    throw new InvalidOperationException($"opcode:{respose.OpCode} should not be handled here");
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            
            
        }
    }
}

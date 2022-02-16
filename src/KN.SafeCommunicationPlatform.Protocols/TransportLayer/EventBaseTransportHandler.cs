using KN.SafeCommunicationPlatform.Protocols.Internal;
using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class EventBaseTransportHandler
    {
        

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0,1);

        private readonly PacketReader packetReader;
        private readonly PacketWriter packetWriter;
        private readonly LossSet _curcor = new LossSet(3);

        private Pipe pipe = new Pipe();


        public event Action<Stream>? Received;

        public event Action? Closed;
        public event Action<Exception>? Errored;
        public EventBaseTransportHandler(IQrReader qrReader, IQrWriter qrWriter)
        {
            packetReader = new PacketReader(qrReader);
            packetWriter = new PacketWriter(qrWriter);
        }

        public void Listen()
        {
            Task.Factory.StartNew(async () =>
            {
                bool end = false;
                await foreach(var packet in packetReader.GetPacketStream())
                {
                    try
                    {
                        AddTime();
                        switch (packet.OpCode)
                        {
                            case OpCode.Close:
                                end = true;
                                break;
                            case OpCode.Send:
                                //await _semaphoreSlim.WaitAsync();
                                await pipe.Writer.WriteAsync(packet.Payload);
                                await SendAck();
                                //_semaphoreSlim.Release();
                                if (packet.EndOfMessage)
                                {
                                    Received?.Invoke(pipe.Reader.AsStream());
                                    pipe.Reset();
                                }
                                break;
                            case OpCode.Ack:
                                //_semaphoreSlim.Release();
                                break;
                            case OpCode.Ping:
                                //await _semaphoreSlim.WaitAsync();
                                await SendPong();
                                //_semaphoreSlim.Release();
                                break;
                            case OpCode.Pong:

                                break;

                        }
                        if (end)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Dispose();
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private async ValueTask SendPong()
        {
            var pong = new Packet
            {
                SessionId = 0,
                PacketId = _curcor.GetLast(),
                OpCode = OpCode.Pong,
                EndOfMessage = true,
                PacketSize = 0,
                Payload = Array.Empty<byte>(),
            };
            await SendCoreAsync(pong);
        }

        private async ValueTask SendAck()
        {
            var ack = new Packet
            {
                SessionId = 0,
                PacketId = _curcor.GetLast(),
                OpCode = OpCode.Ack,
                EndOfMessage = true,
                PacketSize = 0,
                Payload = Array.Empty<byte>(),
            };
            await SendCoreAsync(ack);
        }

        private async ValueTask SendCoreAsync(Packet packet)
        {
            packetWriter.WritePacket(ref packet);
            AddTime();
            await Task.Delay(2);
        }

        private void AddTime()
        {

        }
    }
}

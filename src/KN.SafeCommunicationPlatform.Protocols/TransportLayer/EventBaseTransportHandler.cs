using KN.SafeCommunicationPlatform.Protocols.Internal;
using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class EventBaseTransportHandler
    {
        

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0,1);

        private readonly PacketReader packetReader;
        private readonly PacketWriter packetWriter;
        private readonly LossSet _curcor = new LossSet(3);
        private readonly HashSet<uint> _ackSet = new HashSet<uint>();
        private readonly HashSet<uint> _pongSet = new HashSet<uint>();

        private Pipe pipe = new Pipe();


        public event Func<Stream,Task>? Received;

        public event Func<Task>? Closed;
        public event Func<Exception,Task>? Errored;
        public EventBaseTransportHandler(IQrReader qrReader, IQrWriter qrWriter)
        {
            packetReader = new PacketReader(qrReader);
            packetWriter = new PacketWriter(qrWriter);
        }

        public void Listen()
        {
            _ = Task.Factory.StartNew(async () =>
              {
                  bool end = false;
                  await foreach (var packet in packetReader.GetPacketStream())
                  {
                      try
                      {
                          _curcor.Put(packet.PacketId);
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
                                      await pipe.Writer.CompleteAsync();
                                      if(Received != null)
                                      {
                                          await Received(pipe.Reader.AsStream());
                                      }
                                      pipe.Reset();
                                  }
                                  break;
                              case OpCode.Ack:
                                  _ackSet.Add(packet.PacketId);
                                //_semaphoreSlim.Release();
                                break;
                              case OpCode.Ping:
                                //await _semaphoreSlim.WaitAsync();
                                await SendPong();
                                //_semaphoreSlim.Release();
                                break;
                              case OpCode.Pong:
                                  _pongSet.Add(packet.PacketId);
                                  break;


                          }
                          if (end)
                          {
                              if (Closed != null)
                              {
                                  await Closed();
                              }
                              
                              break;
                          }
                      }
                      catch (Exception ex)
                      {
                          if (Errored != null)
                          {
                              await Errored(ex);
                          }
                      }
                      finally
                      {
                          //_semaphoreSlim.Dispose();
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
            await Task.Delay(200);
        }

        private async ValueTask SendCoreAsync(Packet packet)
        {
            AddTime();
            await packetWriter.WritePacket(packet);
            await Task.Delay(2);
        }

        private void AddTime()
        {

        }

        public async ValueTask SendAsync(Stream stream)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Packet.MaxPayload);
            try
            {
                var mem = new Memory<byte>(buffer).Slice(0, Packet.MaxPayload);
                var read = await stream.ReadAsync(mem);
                while (read > 0)
                {
                    var packet = new Packet
                    {
                        SessionId = 0,
                        PacketId = _curcor.PutNext(),
                        OpCode = OpCode.Send,
                        EndOfMessage = false,
                        PacketSize = (uint)read,
                        Payload = mem[0..read].ToArray()
                    };
                    await SendCoreAsync(packet);
                    await WaitAckAsync(packet.PacketId);
                    read = await stream.ReadAsync(mem);
                }
                var packetfin = new Packet
                {
                    SessionId = 0,
                    PacketId = _curcor.PutNext(),
                    OpCode = OpCode.Send,
                    EndOfMessage = true,
                    PacketSize = 0u,
                    Payload = Array.Empty<byte>()
                };
                await SendCoreAsync(packetfin);
                await WaitAckAsync(packetfin.PacketId);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }
        }

        private async ValueTask WaitAckAsync(uint ack)
        {
            while (true)
            {

                if (_ackSet.TryGetValue(ack, out _))
                {
                    _ackSet.Remove(ack);
                    return;
                }
                else
                {
                    await Task.Delay(20);
                }
            }
            
        }
    }
}

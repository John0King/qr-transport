using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Threading.Tasks.Sources;
using Timer = System.Timers.Timer;
using KN.SafeCommunicationPlatform.Protocols.Internal;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class TransportHandler:IDisposable
    {
        public TransportHandler(IQrReader qrReader, IQrWriter qrWriter)
        {
            QrReader = qrReader;
            QrWriter = qrWriter;
            PacketReader = new PacketReader(qrReader);
            PacketWriter = new PacketWriter(qrWriter);
        }

        private readonly uint _sessionId = 0;

        public Timer Timer = new Timer(10000);

        public IQrReader QrReader { get; }
        public IQrWriter QrWriter { get; }

        private uint _currentPacket = 0;
        private uint _otherSidePacketCount = 0;

        private readonly LossSet _localSet = new LossSet(10);
        private readonly LossSet _remoteSet = new LossSet(3);

        private event Action<Packet>? OnAckClosing;
        private event Action<Packet>? OnPong;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Pipe Pipe { get; } = new Pipe();
        private readonly List<Pipe> _receivePipeQueue = new List<Pipe>() { new Pipe() };
        //public Stream Stream => Pipe.Reader.AsStream();

        //public Stream InputStream => Pipe.Writer.AsStream();

        /// <summary>
        /// Out DataBufffer
        /// </summary>
        private Memory<byte> OutDataBuffer { get; set; }

        private readonly byte[] EmptyArray = new byte[0];

        private PacketReader PacketReader { get; }
        private PacketWriter PacketWriter { get; }

        public async ValueTask SendData(Memory<byte> buffer, bool endOfMessage, CancellationToken cancellationToken)
        {
            int pos = 0;
            int length = Math.Min(buffer.Length, 1024);
            while(pos < buffer.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var buf = buffer.Slice(pos, length);
                var packet = new Packet
                {
                    SessionId = _sessionId,
                    PacketId = _localSet.PutNext(),
                    OpCode = OpCode.Send,
                    EndOfMessage = endOfMessage,
                    PacketSize = (uint)buf.Length,
                };
                SendCore(ref packet);
                pos += length;
                await WaitNextPacketAsync();
            }
            
        }

        public event Action<Exception>? ErrorFired;

        private void OnError(Exception e)
        {
            ErrorFired?.Invoke(e);
        }
        public Task StartListenAsync()
        {
            _currentPacket = 0;
            return Task.Factory.StartNew(async () =>
            {
                await foreach(var packet in PacketReader.GetPacketStream(_cancellationTokenSource.Token))
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    if (_remoteSet.Has(packet.PacketId))
                    {
                        continue;
                    }
                    if(packet.PacketId != _localSet.GetLast() + 1)
                    {
                        
                    }
                    _remoteSet.Put(packet.PacketId);
                    
                    switch (packet.OpCode)
                    {
                        case OpCode.Ping:
                            this.SendPong();
                            break;
                        case OpCode.Pong:
                            this.OnPong?.Invoke(packet);
                            this.AddTime();
                            break;
                        case OpCode.Send:
                            await this.AgragateData(packet);
                            this.SendAck();
                            break;
                        //case OpCode.SendLast:
                        //    await this.ReceiveData(packet, true);
                        //    this.SendAck();
                        //    break;
                        case OpCode.Ack:
                            this.SendNext();
                            break;
                        case OpCode.Repeat:
                            this.Send();
                            break;
                        case OpCode.Close:
                            this.OnAckClosing?.Invoke(packet);
                            this.StopListen();
                            break;
                    }
                }
            }, TaskCreationOptions.LongRunning);

        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask CloseAsync(Exception? exception,CancellationToken cancellationToken)
        {
            var packet = new Packet
            {
                SessionId = _sessionId,
                PacketId = _localSet.PutNext(),
                OpCode = OpCode.Close,
                EndOfMessage = true,
                PacketSize = 0,
            };
            SendCore(ref packet);
            await WaitCloseAckAsync(cancellationToken);
        }

        public async ValueTask<QrReceiveResult> ReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var readerPipe = _receivePipeQueue.First();
            var r = await readerPipe.Reader.ReadAtLeastAsync(buffer.Length, cancellationToken);
            int length = (int)Math.Min(buffer.Length, r.Buffer.Length);
            var result = new QrReceiveResult(length, MessageType.Binary, r.IsCompleted);
            r.Buffer.CopyTo(buffer.Span);
            var nextPos = r.Buffer.GetPosition(length, r.Buffer.Start);
            readerPipe.Reader.AdvanceTo(nextPos);
            if(r.IsCompleted || r.IsCanceled)
            {
                await readerPipe.Reader.CompleteAsync();
                _receivePipeQueue.Remove(readerPipe);
            }
            return result;
        }

        public void StopListen()
        {
            _cancellationTokenSource.Cancel();
        }

#if NONE
        public async Task StartSendAsync()
        {
            this.Timer.Start();
            using var owner = MemoryPool<byte>.Shared.Rent(1040);
            
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                
                var result = await Pipe.Reader.ReadAsync(_cancellationTokenSource.Token);
                OutDataBuffer = owner.Memory;
                if(result.Buffer.Length < OutDataBuffer.Length && !result.IsCompleted)
                {
                    continue;
                }
                OutDataBuffer = OutDataBuffer.Slice(0, (int)Math.Min(result.Buffer.Length,OutDataBuffer.Length));
                result.Buffer.CopyTo(OutDataBuffer.Span);
                this.Send();
                var pos = result.Buffer.GetPosition(OutDataBuffer.Length, result.Buffer.Start);
                Pipe.Reader.AdvanceTo(pos);

                await this.WaitNextPacketAsync();
            }

        }
#endif
        private TaskCompletionSource NextPacketSource = new  TaskCompletionSource();
        private Task WaitNextPacketAsync()
        {
            return NextPacketSource.Task;
        }

        public Task WaitCloseAckAsync(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3000);
            
            var tcs = new TaskCompletionSource();
            OnAckClosing += AckClosing;
            cts.Token.Register(() =>
            {
                tcs.TrySetException(new TaskCanceledException());
            });
            return tcs.Task;
            void AckClosing(Packet packet)
            {
                tcs.TrySetResult();
            }
        }

        public Task WaitPongAsync(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(10000);

            var tcs = new TaskCompletionSource();
            OnAckClosing += AckClosing;
            cts.Token.Register(() =>
            {
                tcs.TrySetException(new TaskCanceledException());
            });
            return tcs.Task;
            void AckClosing(Packet packet)
            {
                tcs.TrySetResult();
            }
        }

        public void SendPing()
        {
            var packet = new Packet
            {
                SessionId = 0,
                PacketId = _currentPacket,
                OpCode = OpCode.Ping,
                PacketSize = 0,
                Payload = EmptyArray
            };
            this.SendCore(ref packet);
        }

        private void SendPong()
        {
            var packet = new Packet
            {
                SessionId = 0,
                PacketId = _currentPacket,
                OpCode = OpCode.Pong,
                PacketSize = 0,
                Payload = EmptyArray
            };
            this.SendCore(ref packet);
        }

        private void AddTime()
        {
            Timer.Stop();
            Timer.Start();
        }

        private void Send()
        {
            var packet = new Packet
            {
                SessionId = 0,
                PacketId = _currentPacket,
                OpCode = OpCode.Send,
                PacketSize = (uint)OutDataBuffer.Length,
                Payload = OutDataBuffer.ToArray()
            };
            this.SendCore(ref packet);
        }
        private void SendNext()
        {
            _currentPacket += 1;
            this.NextPacketSource.SetResult();
            this.NextPacketSource = new TaskCompletionSource();
        }

        private void SendAck()
        {
            var packet = new Packet
            {
                SessionId = 0,
                PacketId = _otherSidePacketCount,
                OpCode = OpCode.Ack,
                PacketSize = 0u,
                Payload = EmptyArray
            };
            this.SendCore(ref packet);
        }


        private async Task AgragateData(Packet packet)
        {
            var writePipe = _receivePipeQueue.Last();
            _otherSidePacketCount = packet.PacketId;
            //Stream.Write(packet.Payload, 0, (int)packet.PacketSize);
            var result = await writePipe.Writer.WriteAsync(packet.Payload.AsMemory(..(int)packet.PacketSize));
            //Pipe.Writer.Advance((int)packet.PacketSize);
            if (packet.EndOfMessage == true)
            {
                await writePipe.Writer.CompleteAsync();
                _receivePipeQueue.Add(new Pipe());
            }
        }


        private void SendCore(ref Packet packet)
        {
            PacketWriter.WritePacket(ref packet);
            AddTime();
        }

        public void Dispose()
        {
            StopListen();
            _cancellationTokenSource.Dispose();
            Timer.Dispose();
            Pipe.Reader.Complete(new OperationCanceledException());
            Pipe.Writer.Complete(new OperationCanceledException());
        }
    }
}

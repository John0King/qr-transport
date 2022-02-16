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
using KN.SafeCommunicationPlatform.Protocols.Internal;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class TransportHandler2 : IDisposable
    {
        public TransportHandler2(IQrReader qrReader, IQrWriter qrWriter)
        {
            PacketReader = new PacketReader(qrReader);
            PacketWriter = new PacketWriter(qrWriter);
            _timer = new Timer(async (_) =>
            {
                await this.PingAsync();
            },null,TimeSpan.Zero,IdleTime);
        }

        private readonly uint _sessionId = 0;

        private PacketReader PacketReader { get; }
        private PacketWriter PacketWriter { get; }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private LossSet _cursor = new LossSet(3);

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0, 1);
        private readonly byte[] Empty = Array.Empty<byte>();
        private DateTimeOffset _aliveTime;

        private DateTimeOffset AliveTime
        {
            get
            {
                return _aliveTime;
            }
            set
            {
                _aliveTime = value;
                _timer.Change(TimeSpan.Zero, IdleTime);
            }
        }
        public TimeSpan IdleTime
        {
            get
            {
                return _idleTime;
            }
            set
            {
                _idleTime = value;
                _timer.Change(TimeSpan.Zero, value);
            }
        }

        private bool IsIdleTimeout => DateTimeOffset.UtcNow - _aliveTime > IdleTime;
        private Timer _timer;
        private Exception? _exception = null;


        private IAsyncEnumerable<Packet> packetsStream = default!;
        private IAsyncEnumerator<Packet> packetItorator = default!;
        private TimeSpan _idleTime = TimeSpan.FromSeconds(15);

       

        public Task Listen()
        {
            packetsStream = PacketReader.GetPacketStream(_cancellationTokenSource.Token);
            packetItorator = packetsStream.GetAsyncEnumerator();
            AliveTime = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }


        /// <summary>
        /// 关闭链接
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns> 
        public async ValueTask CloseAsync(Exception? exception, CancellationToken cancellationToken)
        {
            var packet = new Packet
            {
                SessionId = _sessionId,
                PacketId = _cursor.PutNext(),
                OpCode = OpCode.Close,
                EndOfMessage = true,
                PacketSize = 0,
            };
            SendCore(ref packet);
            await Task.Delay(200);
            //await WaitCloseAckAsync(cancellationToken);
        }


        /// <summary>
        /// 接收信息
        /// </summary>
        /// <param name="buffer">一个不小于<see cref="Packet.MaxPayload"/>的缓冲区</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public async ValueTask<QrReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (buffer.Length < Packet.MaxPayload)
            {
                throw new ArgumentException($"buffer too small, buffer must greater than {Packet.MaxPayload}");
            }
            if (packetsStream == null)
            {
                throw new InvalidOperationException("Transport not Listen yet");
            }
            if(_exception != null)
            {
                var ex = _exception;
                _exception = null;
                throw ex;
            }
            bool keepReceive = true;
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);
                while (keepReceive)
                {
                    var x = await packetItorator.MoveNextAsync();
                    if (x)
                    {
                        _cursor.Put(packetItorator.Current.PacketId);
                        switch (packetItorator.Current.OpCode)
                        {
                            case OpCode.Close:
                                await this.CloseInternalAsync();
                                return new QrReceiveResult(0, MessageType.Close, true);
                            case OpCode.Send:
                                packetItorator.Current.Payload.CopyTo(buffer.Slice(0, (int)packetItorator.Current.PacketSize));
                                await this.SendAckAsync();
                                return new QrReceiveResult((int)packetItorator.Current.PacketSize, MessageType.Binary, packetItorator.Current.EndOfMessage);
                            case OpCode.Ping:
                                await this.SendPongAsync();
                                break;
                            //case OpCode.Repeat:
                            //    var p = new Packet();
                            //    this.SendCore(ref p); //resend
                            //    break;

                            //case OpCode.Pong:
                            //    goto StartProcess; // add time


                            //case OpCode.Ack:
                            //    goto StartProcess; // now you can send
                            default:
                                throw new InvalidDataException($"receive invalid Data");
                        }
                    }
                }
                throw new InvalidDataException("invalid state");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }



        public void StopListen()
        {
            _cancellationTokenSource.Cancel();
        }




        public async ValueTask Send(Memory<byte> buffer, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _semaphoreSlim.WaitAsync(IdleTime))
                {
                    throw new TimeoutException();
                }
                int pos = 0;
                int length = Math.Min(buffer.Length, 1024);
                while (pos < buffer.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var buf = buffer.Slice(pos, length);
                    var packet = new Packet
                    {
                        SessionId = _sessionId,
                        PacketId = _cursor.PutNext(),
                        OpCode = OpCode.Send,
                        EndOfMessage = endOfMessage,
                        PacketSize = (uint)buf.Length,
                    };
                    SendCore(ref packet);

                    await WaitAckAsync();

                    pos += length;
                    //await WaitNextPacketAsync();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }

        }


        #region 发起

        private void SendCore(ref Packet packet)
        {
            AliveTime = DateTimeOffset.UtcNow;
            PacketWriter.WritePacket(ref packet);
        }

        private async ValueTask SendPongAsync()
        {
            var pong = new Packet()
            {
                SessionId = 0,
                PacketId = _cursor.GetLast(),
                OpCode = OpCode.Pong,
                EndOfMessage = true,
                PacketSize = 0,
                Payload = Empty
            };
            this.SendCore(ref pong);
            await Task.Delay(500);
        }
        private async ValueTask SendAckAsync()
        {
            var ack = new Packet()
            {
                SessionId = 0,
                PacketId = _cursor.GetLast(),
                PacketSize = 0,
                EndOfMessage = true,
                OpCode = OpCode.Ack,
                Payload = Empty
            };
            this.SendCore(ref ack);
            await Task.Delay(2);
        }

        private async ValueTask PingAsync()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                var ping = new Packet
                {
                    SessionId = 0,
                    PacketId = _cursor.PutNext(),
                    OpCode = OpCode.Ping,
                    PacketSize = 0,
                    EndOfMessage= true,
                    Payload = Empty
                };
                SendCore(ref ping);
                await WaitPongAsync();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        #endregion

        #region 等待



        private async ValueTask WaitAckAsync()
        {
            if (await Task.WhenAny(packetItorator.MoveNextAsync().AsTask(), Task.Delay(IdleTime)) is Task<bool>)
            {
                if (packetItorator.Current.OpCode == OpCode.Ack)
                {
                    if (packetItorator.Current.PacketId == _cursor.GetLast())
                    {
                        AliveTime = DateTimeOffset.UtcNow;
                        return;
                    }
                    throw new InvalidDataException($"wait ack for ({_cursor.GetLast()}) but got ({packetItorator.Current.PacketId})");
                }
                throw new InvalidDataException($"wait {{{OpCode.Ack}}} but got {{{packetItorator.Current.OpCode}}}");
            }
            else
            {
                throw new TimeoutException();
            }
        }

        private async ValueTask WaitPongAsync()
        {
            if (await Task.WhenAny(packetItorator.MoveNextAsync().AsTask(), Task.Delay(IdleTime)) is Task<bool>)
            {
                if (packetItorator.Current.OpCode == OpCode.Pong)
                {
                    if (packetItorator.Current.PacketId == _cursor.GetLast())
                    {
                        AliveTime = DateTimeOffset.UtcNow;
                        return;
                    }
                    throw new InvalidDataException($"wait ack for ({_cursor.GetLast()}) but got ({packetItorator.Current.PacketId})");
                }
                throw new InvalidDataException($"wait {{{OpCode.Ack}}} but got {{{packetItorator.Current.OpCode}}}");
            }
            else
            {
                var ex = new TimeoutException();
                _exception = ex;
                throw ex;
            }
        }

        private async ValueTask CloseInternalAsync()
        {
            AliveTime = DateTimeOffset.UtcNow;
            await packetItorator.DisposeAsync();
        }

        #endregion

        public async void Dispose()
        {
            StopListen();
            _cancellationTokenSource.Dispose();
            await packetItorator.DisposeAsync();
        }
    }
}

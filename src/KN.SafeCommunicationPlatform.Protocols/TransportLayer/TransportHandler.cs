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
using KN.SafeCommunicationPlatform.Protocols.QrSockets;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class TransportHandler : IDisposable
    {
        public TransportHandler(IQrReader qrReader, IQrWriter qrWriter)
        {
            PacketReader = new PacketReader(qrReader);
            PacketWriter = new PacketWriter(qrWriter);
            _timer = new Timer(static state =>
            {
                var handler = (TransportHandler)state!;
                _ = handler.TrySendPingOnIdleAsync();
            }, this, IdleTime, IdleTime);
        }

        private readonly uint _sessionId = 0;

        private PacketReader PacketReader { get; }
        private PacketWriter PacketWriter { get; }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private LossSet _cursor = new LossSet(3);

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly byte[] Empty = Array.Empty<byte>();
        private const byte FollowerRoleValue = 1;
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
                _timer.Change(IdleTime, IdleTime);
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
                _timer.Change(value, value);
            }
        }

        private bool IsIdleTimeout => DateTimeOffset.UtcNow - _aliveTime > IdleTime;

        public QrSocketState State { get; private set; } = QrSocketState.Closed;
        public QrSocketRole Role { get; private set; } = QrSocketRole.Unknown;
        public bool CanSend => State == QrSocketState.Connected && _canSend;

        private Timer _timer;
        private Exception? _exception = null;


        private IAsyncEnumerable<Packet> packetsStream = default!;
        private IAsyncEnumerator<Packet> packetItorator = default!;
        private TimeSpan _idleTime = TimeSpan.FromSeconds(15);
        private bool _canSend;
        private bool _isSending;
        private bool _isReceiving;

       

        public Task Listen()
        {
            packetsStream = PacketReader.GetPacketStream(_cancellationTokenSource.Token);
            packetItorator = packetsStream.GetAsyncEnumerator();
            AliveTime = DateTimeOffset.UtcNow;
            State = QrSocketState.Connected;
            Role = QrSocketRole.Unknown;
            _canSend = false;
            return Task.CompletedTask;
        }

        public async ValueTask NotifyScannedPeerAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (State != QrSocketState.Connected)
            {
                throw new InvalidOperationException("Connection is not connected");
            }

            if (Role != QrSocketRole.Unknown)
            {
                return;
            }

            var hello = new Packet
            {
                SessionId = _sessionId,
                PacketId = _cursor.PutNext(),
                OpCode = OpCode.Hello,
                EndOfMessage = true,
                PacketSize = 1,
                Payload = new byte[] { FollowerRoleValue },
            };

            await SendCore(hello);
            Role = QrSocketRole.Follower;
            _canSend = false;
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
            await SendCore(packet);
            await Task.Delay(200);
            State = QrSocketState.Closed;
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
                _isReceiving = true;
                await _semaphoreSlim.WaitAsync(cancellationToken);
                while (keepReceive)
                {
                    var x = await packetItorator.MoveNextAsync();
                    State = QrSocketState.Connected;
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
                                if (packetItorator.Current.EndOfMessage)
                                {
                                    _canSend = true;
                                }
                                return new QrReceiveResult((int)packetItorator.Current.PacketSize, MessageType.Binary, packetItorator.Current.EndOfMessage);
                            case OpCode.Hello:
                                ProcessHello(packetItorator.Current);
                                break;
                            case OpCode.Receive:
                                _canSend = true;
                                await this.SendReceiveResultAsync();
                                break;
                            case OpCode.ReceiveResult:
                                break;
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
                _isReceiving = false;
                _semaphoreSlim.Release();
            }
        }



        public void StopListen()
        {
            _cancellationTokenSource.Cancel();
        }




        public async ValueTask Send(Memory<byte> buffer, bool endOfMessage, CancellationToken cancellationToken)
        {
            EnsureCanSend();

            try
            {
                _isSending = true;
                await _semaphoreSlim.WaitAsync(IdleTime);
                int pos = 0;
                int chunkSize = Math.Min(buffer.Length, Packet.MaxPayload);
                while (pos < buffer.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var length = Math.Min(chunkSize, buffer.Length - pos);
                    var buf = buffer.Slice(pos, length);
                    var packet = new Packet
                    {
                        SessionId = _sessionId,
                        PacketId = _cursor.PutNext(),
                        OpCode = OpCode.Send,
                        EndOfMessage = endOfMessage && (pos + length >= buffer.Length),
                        PacketSize = (uint)buf.Length,
                        Payload = buf.ToArray(),
                    };
                    await SendCore(packet);

                    await WaitAckAsync();

                    pos += length;
                    //await WaitNextPacketAsync();
                }

                if (endOfMessage)
                {
                    _canSend = false;
                    await SendReceiveAsync();
                    await WaitReceiveResultAsync();
                }
            }
            finally
            {
                _isSending = false;
                _semaphoreSlim.Release();
            }

        }


        #region 发起

        private async ValueTask SendCore(Packet packet)
        {
            AliveTime = DateTimeOffset.UtcNow;
            await PacketWriter.WritePacket(packet);
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
            await this.SendCore(pong);
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
            await this.SendCore(ack);
            await Task.Delay(2);
        }

        private async ValueTask SendReceiveAsync()
        {
            var receive = new Packet()
            {
                SessionId = 0,
                PacketId = _cursor.PutNext(),
                PacketSize = 0,
                EndOfMessage = true,
                OpCode = OpCode.Receive,
                Payload = Empty
            };
            await this.SendCore(receive);
            await Task.Delay(2);
        }

        private async ValueTask SendReceiveResultAsync()
        {
            var receiveResult = new Packet()
            {
                SessionId = 0,
                PacketId = _cursor.GetLast(),
                PacketSize = 0,
                EndOfMessage = true,
                OpCode = OpCode.ReceiveResult,
                Payload = Empty
            };
            await this.SendCore(receiveResult);
            await Task.Delay(2);
        }

        internal async ValueTask PingAsync()
        {
            if (!CanSendPing())
            {
                return;
            }

            if (!await _semaphoreSlim.WaitAsync(0))
            {
                return;
            }

            try
            {
                var ping = new Packet
                {
                    SessionId = 0,
                    PacketId = _cursor.PutNext(),
                    OpCode = OpCode.Ping,
                    PacketSize = 0,
                    EndOfMessage= true,
                    Payload = Empty
                };
                await SendCore(ping);
                await WaitPongAsync();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task TrySendPingOnIdleAsync()
        {
            if (!CanSendPing())
            {
                return;
            }

            try
            {
                await PingAsync();
            }
            catch
            {
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
                        State = QrSocketState.Connected;
                        return;
                    }
                     State = QrSocketState.Closed;
                    throw new InvalidDataException($"wait ack for ({_cursor.GetLast()}) but got ({packetItorator.Current.PacketId})");
                }
                State = QrSocketState.Closed;
                throw new InvalidDataException($"wait {{{OpCode.Ack}}} but got {{{packetItorator.Current.OpCode}}}");
            }
            else
            {
                State = QrSocketState.Closed;
                throw new TimeoutException();
            }
        }

        private bool CanSendPing()
        {
            return State == QrSocketState.Connected
                && Role == QrSocketRole.Controller
                && IsIdleTimeout
                && !_isSending
                && !_isReceiving;
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
                        State = QrSocketState.Connected;
                        return;
                    }
                     State = QrSocketState.Closed;
                    throw new InvalidDataException($"wait ack for ({_cursor.GetLast()}) but got ({packetItorator.Current.PacketId})");
                }
                State = QrSocketState.Closed;
                throw new InvalidDataException($"wait {{{OpCode.Ack}}} but got {{{packetItorator.Current.OpCode}}}");
            }
            else
            {
                State = QrSocketState.Closed;
                var ex = new TimeoutException();
                _exception = ex;
                throw ex;
            }
        }

        private async ValueTask WaitReceiveResultAsync()
        {
            if (await Task.WhenAny(packetItorator.MoveNextAsync().AsTask(), Task.Delay(IdleTime)) is Task<bool>)
            {
                if (packetItorator.Current.OpCode == OpCode.ReceiveResult)
                {
                    AliveTime = DateTimeOffset.UtcNow;
                    State = QrSocketState.Connected;
                    return;
                }
                State = QrSocketState.Closed;
                throw new InvalidDataException($"wait {{{OpCode.ReceiveResult}}} but got {{{packetItorator.Current.OpCode}}}");
            }
            else
            {
                State = QrSocketState.Closed;
                var ex = new TimeoutException();
                _exception = ex;
                throw ex;
            }
        }

        private async ValueTask CloseInternalAsync()
        {
            AliveTime = DateTimeOffset.UtcNow;
            State = QrSocketState.Closed;
            await packetItorator.DisposeAsync();
        }

        private void ProcessHello(Packet packet)
        {
            if (packet.PacketSize < 1)
            {
                throw new InvalidDataException("hello packet payload invalid");
            }

            if (packet.Payload[0] == FollowerRoleValue)
            {
                Role = QrSocketRole.Controller;
                _canSend = true;
                return;
            }

            throw new InvalidDataException("hello packet role invalid");
        }

        private void EnsureCanSend()
        {
            if (Role == QrSocketRole.Unknown)
            {
                throw new InvalidOperationException("Role is unknown, call NotifyScannedPeerAsync on the scanned side first");
            }

            if (!_canSend)
            {
                throw new InvalidOperationException("Current turn is receive-only");
            }
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

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
    public class TransportHandler2:IDisposable
    {
        public TransportHandler2(IQrReader qrReader, IQrWriter qrWriter)
        {
            PacketReader = new PacketReader(qrReader);
            PacketWriter = new PacketWriter(qrWriter);
        }

        private readonly uint _sessionId = 0;

        private PacketReader PacketReader { get; }
        private PacketWriter PacketWriter { get; }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private LossSet _localSet;



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
                //await WaitNextPacketAsync();
            }
            
        }


        private IAsyncEnumerable<Packet>? packetsStream;

        public Task Listen()
        {
            packetsStream = PacketReader.GetPacketStream(_cancellationTokenSource.Token);
            return Task.CompletedTask;
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
            //await WaitCloseAckAsync(cancellationToken);
        }

        private void CloseInternal()
        {

        }

        public async ValueTask<QrReceiveResult> ReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if(packetsStream == null)
            {
                throw new InvalidOperationException("Transport not Listen yet");
            }
            var eot = packetsStream.GetAsyncEnumerator(cancellationToken);
            StartProcess:
            var x = await eot.MoveNextAsync();
            if (x)
            {
               switch(eot.Current.OpCode)
                {
                    case OpCode.Close:
                        this.CloseInternal();
                        return new QrReceiveResult(0, MessageType.Close, true);
                    case OpCode.Repeat:
                        var p = new Packet();
                        this.SendCore(ref p); //resend
                        break;
                    case OpCode.Ping:
                        var p1 = new Packet();
                        this.SendCore(ref p1);
                        goto StartProcess;
                    case OpCode.Pong:
                        goto StartProcess; // add time

                    case OpCode.Send:
                        eot.Current.Payload.CopyTo(buffer.Slice(0, (int)eot.Current.PacketSize));
                        //this.SendCore(ref p1);//send ack
                        return new QrReceiveResult((int)eot.Current.PacketSize, MessageType.Binary, eot.Current.EndOfMessage);
                    case OpCode.Ack:
                        goto StartProcess; // now you can send
                    default:
                        break;
                }
            }
            return new QrReceiveResult(0,MessageType.Close,true);
            
        }

        public void StopListen()
        {
            _cancellationTokenSource.Cancel();
        }


        private void SendCore(ref Packet packet)
        {
            PacketWriter.WritePacket(ref packet);
        }

        public void Dispose()
        {
            StopListen();
            _cancellationTokenSource.Dispose();
        }
    }
}

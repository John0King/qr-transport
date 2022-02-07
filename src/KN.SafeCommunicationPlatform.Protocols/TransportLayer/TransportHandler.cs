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

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public class TransportHandler
    {

        public static readonly PacketWriter PacketWriter = new PacketWriter();

        

        public TransportHandler(IQrReader qrReader, IQrWriter qrWriter)
        {
            QrReader = qrReader;
            QrWriter = qrWriter;
            PacketReader = new PacketReader(qrReader);
        }

        public Timer Timer = new Timer(10000);

        public IQrReader QrReader { get; }
        public IQrWriter QrWriter { get; }

        private uint _currentPacket = 0;
        private uint _otherSidePacketCount = 0;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Pipe Pipe { get; } = new Pipe();
        public Stream Stream => Pipe.Reader.AsStream();

        public Stream InputStream => Pipe.Writer.AsStream();
        private Memory<byte> TotalBuffer { get; } = new Memory<byte>(new byte[1024]);

        private Memory<byte> DataBuffer { get; set; }
        private Memory<byte> _packetBuffer { get; set; } = new Memory<byte>(new byte[1040]);

        private byte[] EmptyArray = new byte[0];

        private PacketReader PacketReader { get; }

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
                    switch (packet.OpCode)
                    {
                        case OpCode.Ping:
                            this.SendPong();
                            break;
                        case OpCode.Pong:
                            this.AddTime();
                            break;
                        case OpCode.Send:
                            await this.ReceiveData(packet, false);
                            this.SendAck();
                            break;
                        case OpCode.SendLast:
                            await this.ReceiveData(packet, true);
                            this.SendAck();
                            break;
                        case OpCode.Ack:
                            this.SendNext();
                            break;
                        case OpCode.Repeat:
                            this.Send();
                            break;
                    }
                }
            }, TaskCreationOptions.LongRunning);

        }


        public void StopListen()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task StartSendAsync()
        {
            this.Timer.Start();
            using var owner = MemoryPool<byte>.Shared.Rent(1040);
            
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                
                var result = await Pipe.Reader.ReadAsync(_cancellationTokenSource.Token);
                DataBuffer = owner.Memory;
                if(result.Buffer.Length < DataBuffer.Length && !result.IsCompleted)
                {
                    continue;
                }
                DataBuffer = DataBuffer.Slice(0, (int)Math.Min(result.Buffer.Length,DataBuffer.Length));
                result.Buffer.CopyTo(DataBuffer.Span);
                this.Send();
                var pos = result.Buffer.GetPosition(DataBuffer.Length, result.Buffer.Start);
                Pipe.Reader.AdvanceTo(pos);

                await this.WaitNextPacketAsync();
                //void Send()
                //{
                //    var reader = new System.Buffers.SequenceReader<byte>(result.Buffer);
                //    reader.
                //}
            }

        }

        private TaskCompletionSource NextPacketSource = new  TaskCompletionSource();
        private Task WaitNextPacketAsync()
        {
            return NextPacketSource.Task;
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
                PacketSize = (uint)DataBuffer.Length,
                Payload = DataBuffer.ToArray()
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


        private async Task ReceiveData(Packet packet, bool end)
        {
            _otherSidePacketCount = packet.PacketId;
            //Stream.Write(packet.Payload, 0, (int)packet.PacketSize);
            var result = await Pipe.Writer.WriteAsync(packet.Payload.AsMemory(..(int)packet.PacketSize));
            //Pipe.Writer.Advance((int)packet.PacketSize);
            if (end == true)
            {
                await Pipe.Writer.CompleteAsync();
            }
        }


        private void SendCore(ref Packet packet)
        {
            
            var bytesWrite = PacketWriter.Write(packet, _packetBuffer.Span);
            QrWriter.WriteQrData(_packetBuffer.Span.Slice(0, bytesWrite));
        }
        
    }
}

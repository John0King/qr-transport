using KN.SafeCommunicationPlatform.Protocols;
using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace KN.SafeCommnunicationPlatform.Protocols.Test
{
    public class QrConnectionTest
    {
        [Fact]
        public async Task Test1()
        {
            var qr = new TestQrHandler();
            var connection =  QrConnection.CreateBuilder()
                .WithQrHandler(qr)
                .Build();

            await connection.ConnectAsync();

            
        }


        public class TestQrHandler : IQrHandler
        {
            public MemoryStream StreamPush = new MemoryStream();

            public MemoryStream StreamRead = new MemoryStream();

            readonly Channel<IMemoryOwner<byte>> Channel = System.Threading.Channels.Channel.CreateUnbounded<IMemoryOwner<byte>>() ;
            public void PushQr(ReadOnlySpan<byte> buffer)
            {
                StreamPush.Write(buffer);
            }

            public IMemoryOwner<byte> ReadQr()
            {
                var owner =  MemoryPool<byte>.Shared.Rent(1040);
                while(StreamRead.Length - StreamRead.Position < 1040)
                {
                    Thread.Sleep(500);
                }
                StreamRead.Read(owner.Memory.Span);
                return owner;
            }
        }
    }


    
}
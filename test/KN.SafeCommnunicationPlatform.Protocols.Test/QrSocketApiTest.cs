using KN.SafeCommunicationPlatform.Protocols.Qr;
using KN.SafeCommunicationPlatform.Protocols.QrSockets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KN.SafeCommnunicationPlatform.Protocols.Test
{
    public class QrSocketApiTest
    {
        [Fact]
        public async Task CreateAsync_ShouldReturnConnectedSocket()
        {
            var reader = new FakeQrReader();
            var writer = new FakeQrWriter();
            var factory = new QrSocketFactory(reader, writer);

            using var socket = await factory.CreateAsync();

            Assert.True(reader.IsStarted);
            Assert.Equal(QrSocketState.Connected, socket.State);
        }

        [Fact]
        public async Task SendAsync_WhenSocketClosed_ShouldThrowInvalidOperationException()
        {
            var reader = new FakeQrReader();
            var writer = new FakeQrWriter();
            var factory = new QrSocketFactory(reader, writer);
            using var socket = await factory.CreateAsync();

            await socket.CloseAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await socket.SendAsync(new byte[] { 1, 2, 3 }, true);
            });
        }

        [Fact]
        public async Task SendAsync_WithoutHandshake_ShouldThrowInvalidOperationException()
        {
            var reader = new FakeQrReader();
            var writer = new FakeQrWriter();
            var factory = new QrSocketFactory(reader, writer);
            using var socket = await factory.CreateAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await socket.SendAsync(new byte[] { 1, 2, 3 }, true);
            });

            Assert.Equal("Role is unknown, call NotifyScannedPeerAsync on the scanned side first", ex.Message);
        }

        [Fact]
        public async Task NotifyScannedPeerAsync_ShouldSetFollowerRoleAndReceiveOnlyTurn()
        {
            var reader = new FakeQrReader();
            var writer = new FakeQrWriter();
            var factory = new QrSocketFactory(reader, writer);
            using var socket = await factory.CreateAsync();

            await socket.NotifyScannedPeerAsync();

            Assert.Equal(QrSocketRole.Follower, socket.Role);
            Assert.False(socket.CanSend);
        }

        private sealed class FakeQrReader : IQrReader
        {
            public bool IsStarted { get; private set; }

            public void Dispose()
            {
            }

            public async IAsyncEnumerable<Memory<byte>> GetQrStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => tcs.TrySetResult()))
                {
                    await tcs.Task;
                }

                yield break;
            }

            public void StartRead()
            {
                IsStarted = true;
            }

            public void StopRead()
            {
                IsStarted = false;
            }
        }

        private sealed class FakeQrWriter : IQrWriter
        {
            public void WriteQrData(ReadOnlySpan<byte> qrData)
            {
            }
        }
    }
}

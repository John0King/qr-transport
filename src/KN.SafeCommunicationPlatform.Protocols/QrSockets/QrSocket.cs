using KN.SafeCommunicationPlatform.Protocols.Qr;
using KN.SafeCommunicationPlatform.Protocols.TransportLayer;

namespace KN.SafeCommunicationPlatform.Protocols.QrSockets
{
    public sealed class QrSocket : IDisposable
    {
        private readonly IQrReader _reader;
        private readonly IQrWriter _writer;
        private TransportHandler? _transportHandler;

        internal QrSocket(IQrReader reader, IQrWriter writer)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(writer);

            _reader = reader;
            _writer = writer;
        }

        public QrSocketState State => _transportHandler?.State ?? QrSocketState.Closed;

        public QrSocketRole Role => _transportHandler?.Role ?? QrSocketRole.Unknown;

        public bool CanSend => _transportHandler?.CanSend ?? false;

        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _transportHandler ??= new TransportHandler(_reader, _writer);
            await _transportHandler.Listen();
        }

        public async ValueTask<ReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var handler = GetConnectedHandler();
            var result = await handler.ReceiveAsync(buffer, cancellationToken);
            return ReceiveResult.From(result);
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> buffer, bool endOfMessage = true, CancellationToken cancellationToken = default)
        {
            return GetConnectedHandler().Send(buffer.ToArray(), endOfMessage, cancellationToken);
        }

        public ValueTask CloseAsync(Exception? exception = null, CancellationToken cancellationToken = default)
        {
            return GetConnectedHandler().CloseAsync(exception, cancellationToken);
        }

        public ValueTask NotifyScannedPeerAsync(CancellationToken cancellationToken = default)
        {
            return GetConnectedHandler().NotifyScannedPeerAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transportHandler?.Dispose();
            _transportHandler = null;
        }

        private TransportHandler GetConnectedHandler()
        {
            if (_transportHandler == null || _transportHandler.State != QrSocketState.Connected)
            {
                throw new InvalidOperationException("Socket is not connected");
            }

            return _transportHandler;
        }
    }
}

using KN.SafeCommunicationPlatform.Protocols.Qr;

namespace KN.SafeCommunicationPlatform.Protocols.QrSockets
{
    public sealed class QrSocketFactory
    {
        private readonly IQrReader _reader;
        private readonly IQrWriter _writer;

        public QrSocketFactory(IQrReader reader, IQrWriter writer)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(writer);

            _reader = reader;
            _writer = writer;
        }

        public ValueTask<QrSocket> CreateAsync(CancellationToken cancellationToken = default)
        {
            return AcceptAsync(cancellationToken);
        }

        public async ValueTask<QrSocket> AcceptAsync(CancellationToken cancellationToken = default)
        {
            _reader.StartRead();

            var socket = new QrSocket(_reader, _writer);
            await socket.ConnectAsync(cancellationToken);
            return socket;
        }
    }
}

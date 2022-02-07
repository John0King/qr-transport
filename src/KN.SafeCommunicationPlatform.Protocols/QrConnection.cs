using KN.SafeCommunicationPlatform.Protocols.Qr;
using KN.SafeCommunicationPlatform.Protocols.TransportLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;

namespace KN.SafeCommunicationPlatform.Protocols
{
    public class QrConnection
    {
        public IQrHandler QrHandler { get; internal set; } = default!;

        public PacketWriter PacketWriter { get; private set; } = default!;

        private TransportHandler? _transportHandler;
        private IQrHandler _qrHandler = default!;

        private QrConnection() { }
        public static IQrConnectionBuilder CreateBuilder()
        {
            return new QrConnectionBuilder(()=>new QrConnection());
        }

        public ValueTask ConnectAsync()
        {
            _transportHandler = new TransportHandler(_qrHandler);

            _ = _transportHandler.StartListenAsync();
            _ = _transportHandler.StartSendAsync();

            return ValueTask.CompletedTask;
        }

        public Stream ReadStream => _transportHandler?.Stream!;
        public Stream WriteStream => _transportHandler?.InputStream!;

        public PipeReader PipeReader => _transportHandler?.Pipe.Reader!;

        public PipeWriter PipeWriter => _transportHandler?.Pipe.Writer!;
    }
}

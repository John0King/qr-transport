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
    public class QrConnection:IDisposable
    {

        internal IQrReader QrReader { get; set; } = default!;
        internal IQrWriter QrWriter { get; set; } = default!;

        private TransportHandler? _transportHandler;

        private QrConnection() { }

        public QrConnectionState State { get; private set; }
        public static IQrConnectionBuilder CreateBuilder()
        {
            return new QrConnectionBuilder(()=>new QrConnection());
        }

        public ValueTask ListenAsync()
        {
            _transportHandler = _transportHandler??new TransportHandler(QrReader, QrWriter);
            _ = _transportHandler.StartListenAsync();
            return ValueTask.CompletedTask;
        }
        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            _transportHandler = _transportHandler ?? new TransportHandler(QrReader, QrWriter);

            _ = _transportHandler.StartListenAsync();
            //_ = _transportHandler.StartSendAsync();
            _transportHandler.SendPing();
            await _transportHandler.WaitPongAsync(cancellationToken);
            State = QrConnectionState.Connected;
        }

        public void Dispose()
        {
            _transportHandler?.Dispose();
            _transportHandler=null;
        }

        public async ValueTask SendAsync(Memory<byte> buffer, bool endOfMessage, CancellationToken cancellationToken = default)
        {
            
            if(!IsConnected())
            {
                throw new InvalidOperationException("Connection is not connected");
            }
            await _transportHandler.SendData(buffer, endOfMessage, cancellationToken);
        }

        public async ValueTask<QrReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!IsConnected())
            {
                throw new InvalidOperationException("Connection is not connected");
            }
            return await _transportHandler.ReceiveDataAsync(buffer, cancellationToken);
        }

        public async ValueTask CloseAsync(Exception? exception = null, CancellationToken cancellationToken = default)
        {
            if (IsConnected())
            {
                await _transportHandler.CloseAsync(exception, cancellationToken);
            }
        }


        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "_transportHandler")]
        private bool IsConnected()
        {
            return State == QrConnectionState.Connected;
        }

    }

    public enum QrConnectionState
    {
        Closed,
        Connected
    }
}

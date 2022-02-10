using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    internal class QrConnectionBuilder : IQrConnectionBuilder
    {
        public QrConnectionBuilder(Func<QrConnection> builder)
        {
            _builder = builder;
        }
        private readonly Func<QrConnection> _builder;
        private IQrReader? _qrReader;
        private IQrWriter? _qrWriter;

        public QrConnection Build()
        {
            var conn = _builder();
            conn.QrReader = _qrReader!;
            conn.QrWriter = _qrWriter!;
            return conn;
        }

        public IQrConnectionBuilder WithQrReader(IQrReader qrReader)
        {
            _qrReader = qrReader;
            return this;
        }

        public IQrConnectionBuilder WithQrWriter(IQrWriter qrWriter)
        {
            _qrWriter = qrWriter;
            return this;
        }
    }
}

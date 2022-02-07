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
        private IQrHandler? _qrHander;
        private readonly Func<QrConnection> _builder;

        public IQrConnectionBuilder WithQrHandler(IQrHandler qrHandler)
        {
            _qrHander = qrHandler;
            return this;
        }

        public QrConnection Build()
        {
            var conn = _builder();
            conn.QrHandler = _qrHander!;
            return conn;
        }
    }
}

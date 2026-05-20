using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.QrSockets
{
    public class QrSocketFactory
    {
        private readonly IQrReader _reader;
        private readonly IQrWriter _writer;

        public QrSocketFactory(IQrReader reader, IQrWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public ValueTask<bool> ConnectAsync()
        {
            _reader.StartRead();
            return ValueTask.FromResult(true);
        }

        public ValueTask<QrSocket> AcceptAsync()
        {
            return ValueTask.FromResult(new QrSocket())
        }
        
    }
}

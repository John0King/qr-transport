using KN.SafeCommunicationPlatform.Protocols.Qr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.QrSockets
{
    public class QrSocket
    {
        private readonly IQrReader _reader;
        private readonly IQrWriter _writer;

        private QrSocket(IAsyncEnumerable<byte> reader, IQrWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }


        public async ValueTask<ReciveResult> ReceiveAsync(Memory<byte> bytes)
        {
            await foreach(var x in _reader.GetQrStream())
            {
                
            }
        }

        public async ValueTask SendAsync(Memory<byte> bytes, bool finish = true)
        {

        }
    }
}

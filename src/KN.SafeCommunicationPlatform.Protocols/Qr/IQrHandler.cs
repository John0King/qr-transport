using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.Qr
{
    [Obsolete("do not use this, use IQrReader and IQrWriter instead",true)]
    public interface IQrHandler
    {
        void PushQr(ReadOnlySpan<byte> buffer);

        IMemoryOwner<byte> ReadQr();
    }
}

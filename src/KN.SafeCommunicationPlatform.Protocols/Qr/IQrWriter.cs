using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.Qr
{
    public interface IQrWriter
    {
        void WriteQrData(ReadOnlySpan<byte> qrData);
    }
}

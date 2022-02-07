using KN.SafeCommunicationPlatform.Protocols.TransportLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.Qr
{
    public interface IQrReader : IDisposable
    {
        void StartRead();
        void StopRead();

        /// <summary>
        /// Get Qr Data per Frame
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<Memory<byte>> GetQrStream(CancellationToken cancellationToken = default);

    }
}

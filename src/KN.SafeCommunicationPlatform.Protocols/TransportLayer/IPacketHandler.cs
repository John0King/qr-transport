using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer
{
    public interface IPacketHandler
    {
        IAsyncEnumerable<Packet> GetPackets(CancellationToken cancellationToken = default);

        Packet ReadPacket();

        void SendPacket(ref Packet packet);

    }
}

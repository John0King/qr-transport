using KN.SafeCommunicationPlatform.Protocols.TransportLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace KN.SafeCommnunicationPlatform.Protocols.Test
{
    public class OtherTest
    {
        [Fact]
        public void A()
        {
            var g = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var a = new Packet2
            {
                CtrlCode = CtrlCode.Ping,
                PacketId = 1,
                PacketSize = 1,
                Playload = g,
                SessionId = 99,
            };

            var x = Unsafe.SizeOf<Packet2>();
            Span<byte> sp = new byte[1000];
            MemoryMarshal.Write(sp, ref a);

            var p = a.Playload.Slice(0, 9);

            Assert.Equal(1000, x);
            Assert.True(p.SequenceEqual(g));


            var b = MemoryMarshal.Read<Packet2>(sp);

            Assert.Equal(99u, b.SessionId);

            Assert.True(b.Playload.SequenceEqual(a.Playload));

        }
    }
}

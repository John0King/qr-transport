using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.Internal
{
    public class LossSet
    {
        private readonly ConcurrentQueue<uint> _set = new ();
        public void Put(uint id)
        {
            _set.Enqueue (id);
            if(_set.Count > 10)
            {
               _set.TryDequeue(out _);
            }
        }

        public uint GetLast()
        {
           return _set.LastOrDefault();
        }

        public bool Has(uint v)
        {
            return _set.Contains(v);
        }
    }
}

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
        private readonly object _lock = new object ();
        private readonly int _maxItem;

        public LossSet(int maxItem)
        {
            _maxItem = maxItem;
        }

        public void Put(uint id)
        {
            _set.Enqueue (id);
            if(_set.Count > _maxItem)
            {
               _set.TryDequeue(out _);
            }
        }

        public uint PutNext()
        {
            lock (_lock)
            {
                var x = GetLast();
                x++;
                Put(x);
                return x;
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

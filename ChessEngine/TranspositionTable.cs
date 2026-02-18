using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    using System;
    using System.Collections.Generic;

    public struct Entry : IComparable<Entry>
    {
        public ulong Hash { get; set; }
        public int Depth { get; set; }
        public byte BestMoveIndex { get; set; }

        public int CompareTo(Entry other)
        {
            return Hash.CompareTo(other.Hash);
        }
    }

    public class TranspositionTable
    {
        private readonly HashSet<Entry> _entries = new HashSet<Entry>();

        public void AddEntry(Entry entry)
        {
            Entry hashSetEntry;
            if (_entries.TryGetValue(entry, out hashSetEntry))
            {
                if (hashSetEntry.Depth < entry.Depth)
                {
                    _entries.Remove(hashSetEntry);
                    _entries.Add(entry);
                }
            }
            else
            {
                _entries.Add(entry);
            }
        }

        public byte TryToFindBestMoveIndex(ulong hash)
        {
            var dummyEntry = new Entry { Hash = hash };
            Entry hashSetEntry;
            if (_entries.TryGetValue(dummyEntry, out hashSetEntry))
            {
                return hashSetEntry.BestMoveIndex;
            }
            else
            {
                return byte.MaxValue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public class RepetitionHistory
    {
        private List<ZobristHash> _hashes = new List<ZobristHash>();
        public List<ZobristHash> Hashes { get { return _hashes; } }

        public void AddPosition(ZobristHash hash)
        {
            _hashes.Add(hash.Clone());
        }

        public void Clear()
        {
            _hashes.Clear();
        }

        public int GetRepetitionNumber(ZobristHash hash)
        {
            return _hashes.Count(h => h == hash);
        }

        public RepetitionHistory Clone()
        {
            RepetitionHistory result = new RepetitionHistory();
            foreach(var hash in _hashes)
            {
                result.AddPosition(hash);
            }
            return result;
        }
    }
}

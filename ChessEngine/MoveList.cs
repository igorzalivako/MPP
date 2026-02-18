using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public class MoveList
    {
        private Move[] _moves = new Move[128]; // Начальный размер как в типичных шахматных движках
        private int _size;

        public Move this[int index]
        {
            get => _moves[index];
            set => _moves[index] = value;
        }

        public void PushBack(Move move)
        {
            // Автоматическое расширение массива при необходимости
            if (_size >= _moves.Length)
                Array.Resize(ref _moves, _moves.Length * 2);

            _moves[_size++] = move;
        }

        public int Size => _size;

        // Дополнительные методы для совместимости с LINQ
        public IEnumerable<Move> Enumerate() => _moves.Take(_size);
        public void Clear() => _size = 0;
        public Move? FirstOrDefault(Func<Move, bool> func)
        {
            for (int i = 0; i < _size; i++)
            {
                if (func(_moves[i]))
                {
                    return _moves[i];
                }
            }
            return null;
        }
    }
}

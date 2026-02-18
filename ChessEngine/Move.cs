using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public struct Move : IEquatable<Move>
    {
        public byte From;
        public byte To;
        public PieceType AttackerType;
        public PieceColor AttackerSide;
        public PieceType DefenderType;
        public PieceColor DefenderSide;
        public MoveFlag Flag;

        public Move(byte from, byte to, PieceType attackerType, PieceColor attackerSide,
                    PieceType defenderType, PieceColor defenderSide, MoveFlag flag = MoveFlag.Default)
        {
            From = from;
            To = to;
            AttackerType = attackerType;
            AttackerSide = attackerSide;
            DefenderType = defenderType;
            DefenderSide = defenderSide;
            Flag = flag;
        }

        public bool Equals(Move other)
        {
            return From == other.From
                && To == other.To
                && AttackerType == other.AttackerType
                && AttackerSide == other.AttackerSide
                && DefenderType == other.DefenderType
                && DefenderSide == other.DefenderSide
                && Flag == other.Flag;
        }

        public override bool Equals(object obj) => obj is Move other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To, (int)AttackerType, (int)AttackerSide,
                                  (int)DefenderType, (int)DefenderSide, (int)Flag);
        }

        public static bool operator ==(Move left, Move right) => left.Equals(right);
        public static bool operator !=(Move left, Move right) => !(left == right);
    }

    // Вспомогательные типы
    public enum MoveFlag
    {
        Default,
        PawnLongMove,
        EnPassantCapture,
        WhiteLongCastling,
        WhiteShortCastling,
        BlackLongCastling,
        BlackShortCastling,
        PromoteToKnight,
        PromoteToBishop,
        PromoteToQueen,
        PromoteToRook
    }

    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteLong = 1,
        WhiteShort = 2,
        BlackLong = 4,
        BlackShort = 8
    }
}

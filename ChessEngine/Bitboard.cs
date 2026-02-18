using System.Numerics;

namespace ChessEngine
{
    public struct Bitboard
    {
        public ulong Value;

        public static byte FindMostSignificantBit(ulong bb)
        {
            return (byte)(bb == 0 ? -1 : 63 - BitOperations.LeadingZeroCount(bb));
        }
    }

    public enum PieceColor : byte
    {
        White,
        Black,
        None
    }

    public enum PieceType : byte
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
        None
    }

    public class Pieces
    {
        public Bitboard[,] PieceBitboards; // [color, type]
        public Bitboard[] SideBitboards;   // [color]
        public Bitboard[] InversionSideBitboards;
        public Bitboard All;
        public Bitboard Empty;

        public Pieces(string shortFen)
        {
            PieceBitboards = new Bitboard[2, 6];
            SideBitboards = new Bitboard[2];
            InversionSideBitboards = new Bitboard[2];
            All.Value = 0;
            Empty.Value = ~All.Value;

            int x = 0;
            int y = 7;

            foreach (char c in shortFen)
            {
                if (c == '/')
                {
                    x = 0;
                    y--;
                }
                else if (char.IsDigit(c))
                {
                    x += c - '0';
                }
                else
                {
                    PieceColor side = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                    char pieceChar = char.ToLower(c);

                    int pieceIndex = pieceChar switch
                    {
                        'p' => (int)PieceType.Pawn,
                        'n' => (int)PieceType.Knight,
                        'b' => (int)PieceType.Bishop,
                        'r' => (int)PieceType.Rook,
                        'q' => (int)PieceType.Queen,
                        'k' => (int)PieceType.King,
                        _ => throw new ArgumentException("Invalid FEN character")
                    };

                    int square = y * 8 + x;
                    PieceBitboards[(int)side, pieceIndex].Value |= 1UL << square;
                    x++;
                }
            }

            UpdateBitboards();
        }

        public Pieces(Bitboard all, Bitboard empty, Bitboard[] sideBitboards, Bitboard[] inversionSideBitboards, Bitboard[,] pieceBitboards)
        {
            All = all;
            Empty = empty;
            SideBitboards = sideBitboards;
            InversionSideBitboards = inversionSideBitboards;
            PieceBitboards = pieceBitboards;
        }

        public void UpdateBitboards()
        {
            for (int color = 0; color < 2; color++)
            {
                SideBitboards[color].Value = 0;
                for (int type = 0; type < 6; type++)
                    SideBitboards[color].Value |= PieceBitboards[color, type].Value;

                InversionSideBitboards[color].Value = ~SideBitboards[color].Value;
            }

            All.Value = SideBitboards[0].Value | SideBitboards[1].Value;
            Empty.Value = ~All.Value;
        }

        public static PieceColor Inverse(PieceColor side)
        {
            return side == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        public override bool Equals(object obj)
        {
            return obj is Pieces other && this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PieceBitboards, SideBitboards, All);
        }

        public static bool operator ==(Pieces left, Pieces right)
        {
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 6; j++)
                    if (left.PieceBitboards[i, j].Value != right.PieceBitboards[i, j].Value)
                        return false;

            return true;
        }

        public static bool operator !=(Pieces left, Pieces right)
        {
            return !(left == right);
        }

        public Pieces Clone()
        {
            return new Pieces(All, Empty, (Bitboard[])SideBitboards.Clone(), (Bitboard[])InversionSideBitboards.Clone(), (Bitboard[,])PieceBitboards.Clone());
        }
    }
}

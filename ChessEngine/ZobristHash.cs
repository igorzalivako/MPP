using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public static class ZobristConstants
    {
        public static readonly ulong[,,] PieceSquare;
        public static readonly ulong BlackMove;
        public static readonly ulong WhiteLongCastling;
        public static readonly ulong WhiteShortCastling;
        public static readonly ulong BlackLongCastling;
        public static readonly ulong BlackShortCastling;

        private const ulong SEED = 0x98f107;
        private const ulong MULTIPLIER = 0x71abc9;
        private const ulong SUMMAND = 0xff1b3f;

        static ZobristConstants()
        {
            PieceSquare = new ulong[64, 2, 6];
            ulong previous = SEED;

            // Заполнение 3D-массива для PieceSquare
            for (int square = 0; square < 64; square++)
            {
                for (int side = 0; side < 2; side++)
                {
                    for (int type = 0; type < 6; type++)
                    {
                        previous = NextRandom(previous);
                        PieceSquare[square, side, type] = previous;
                    }
                }
            }

            // Генерация дополнительных констант
            BlackMove = NextRandom(PieceSquare[63, 1, 5]);
            WhiteLongCastling = NextRandom(BlackMove);
            WhiteShortCastling = NextRandom(WhiteLongCastling);
            BlackLongCastling = NextRandom(WhiteShortCastling);
            BlackShortCastling = NextRandom(BlackLongCastling);
        }

        private static ulong NextRandom(ulong previous)
        {
            return MULTIPLIER * previous + SUMMAND;
        }
    }

    public class ZobristHash : IEquatable<ZobristHash>, IComparable<ZobristHash>
    {
        public ulong Hash;
        public ZobristHash(ulong Hash)
        {
            this.Hash = Hash;
        }

        public ZobristHash(Pieces pieces, bool blackMove, bool wLCastling, bool wSCastling, bool bLCastling, bool bSCastling)
        {
            Hash = 0;

            if (blackMove) InvertMove();
            if (wLCastling) InvertWhiteLongCastling();
            if (wSCastling) InvertWhiteShortCastling();
            if (bLCastling) InvertBlackLongCastling();
            if (bSCastling) InvertBlackShortCastling();

            for (byte square = 0; square < 64; square++)
            {
                PieceColor side = PieceColor.None;
                if ((pieces.SideBitboards[0].Value & (1UL << square)) != 0) side = PieceColor.White;
                else if ((pieces.SideBitboards[1].Value & (1UL << square)) != 0) side = PieceColor.Black;

                if (side == PieceColor.None) continue;

                for (byte type = 0; type < 6; type++)
                {
                    if ((pieces.PieceBitboards[(int)side, type].Value & (1UL << square)) != 0)
                    {
                        InvertPiece(square, (PieceType)type, side);
                        break;
                    }
                }
            }
        }

        public void InvertPiece(byte square, PieceType type, PieceColor side)
        {
            Hash ^= ZobristConstants.PieceSquare[square, (int)side, (int)type];
        }

        public void InvertMove() => Hash ^= ZobristConstants.BlackMove;
        public void InvertWhiteLongCastling() => Hash ^= ZobristConstants.WhiteLongCastling;
        public void InvertWhiteShortCastling() => Hash ^= ZobristConstants.WhiteShortCastling;
        public void InvertBlackLongCastling() => Hash ^= ZobristConstants.BlackLongCastling;
        public void InvertBlackShortCastling() => Hash ^= ZobristConstants.BlackShortCastling;

        public bool Equals(ZobristHash other) => Hash == other.Hash;
        public override bool Equals(object? obj) => obj is ZobristHash other && Equals(other);
        public override int GetHashCode() => Hash.GetHashCode();

        public int CompareTo(ZobristHash other) => Hash.CompareTo(other.Hash);

        public static bool operator ==(ZobristHash left, ZobristHash right) => left.Equals(right);
        public static bool operator !=(ZobristHash left, ZobristHash right) => !left.Equals(right);
        public ZobristHash Clone() => new ZobristHash(Hash);
    }
}

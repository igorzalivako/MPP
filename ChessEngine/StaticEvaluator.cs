using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    using System;
    using System.Numerics;

    public static class StaticEvaluator
    {
        private static class Material
        {
            public const int Pawn = 100;
            public const int Knight = 300;
            public const int Bishop = 300;
            public const int Rook = 500;
            public const int Queen = 900;
        }

        private static class Mobility
        {
            public const int Knight = 2;
            public const int Bishop = 3;
            public const int Rook = 4;
            public const int Queen = 5;
        }

        private static class Pawns
        {
            public const int DoublePawn = -10;
            public const int ConnectedPawn = 5;
            public static readonly int[] DefaultPawnPromotion = { 0, 5, 10, 20, 40, 80, 160, 0 };
            public static readonly int[] PassedPawnPromotion = { 0, 10, 20, 40, 80, 160, 320, 0 };
            public const int PawnShield = 15;
        }

        private static class Castlings
        {
            public const int CrashedCastling = -25;
        }

        private static class Bishops
        {
            public const int TwoBishops = 30;
        }

        private static class Endspiel
        {
            public const int MaximumPiecesForEndspiel = 10;
            public const int AttackerKingProximityToDefenderKing = 4;
            public const int DistanceBetweenDefenderKingAndMiddle = -3;
        }

        private static ulong[] whitePassedPawnMasks = new ulong[64];
        private static ulong[] blackPassedPawnMasks = new ulong[64];
        private static ulong[] whitePawnShieldMasks = new ulong[64];
        private static ulong[] blackPawnShieldMasks = new ulong[64];
        private static ulong[] columns = new ulong[8];

        private static int MaterialEvaluation(Pieces pieces)
        {
            int material = 0;
            material += Material.Pawn * (BitOperations.PopCount(pieces.PieceBitboards[0, 0].Value) - BitOperations.PopCount(pieces.PieceBitboards[1, 0].Value));
            material += Material.Knight * (BitOperations.PopCount(pieces.PieceBitboards[0, 1].Value) - BitOperations.PopCount(pieces.PieceBitboards[1, 1].Value));
            material += Material.Bishop * (BitOperations.PopCount(pieces.PieceBitboards[0, 2].Value) - BitOperations.PopCount(pieces.PieceBitboards[1, 2].Value));
            material += Material.Rook * (BitOperations.PopCount(pieces.PieceBitboards[0, 3].Value) - BitOperations.PopCount(pieces.PieceBitboards[1, 3].Value));
            material += Material.Queen * (BitOperations.PopCount(pieces.PieceBitboards[0, 4].Value) - BitOperations.PopCount(pieces.PieceBitboards[1, 4].Value));
            return material;
        }

        private static int CountMoves(ulong mask, Pieces pieces, Func<Pieces, byte, PieceColor, bool, ulong> generator)
        {
            int moves = 0;
            while (mask != 0)
            {
                int index = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1UL << index);
                moves += BitOperations.PopCount(generator(pieces, (byte)index, PieceColor.White, false));
            }
            return moves;
        }

        private static int MobilityEvaluation(Pieces pieces)
        {
            int mobility = 0;
            int knightMoves = CountMoves(pieces.PieceBitboards[0, 1].Value, pieces, PsLegalMoves.GenerateKnightMask);
            int bishopMoves = CountMoves(pieces.PieceBitboards[0, 2].Value, pieces, PsLegalMoves.GenerateBishopMask);
            int rookMoves = CountMoves(pieces.PieceBitboards[0, 3].Value, pieces, PsLegalMoves.GenerateRookMask);
            int queenMoves = CountMoves(pieces.PieceBitboards[0, 4].Value, pieces, PsLegalMoves.GenerateQueenMask);

            knightMoves -= CountMoves(pieces.PieceBitboards[1, 1].Value, pieces, PsLegalMoves.GenerateKnightMask);
            bishopMoves -= CountMoves(pieces.PieceBitboards[1, 2].Value, pieces, PsLegalMoves.GenerateBishopMask);
            rookMoves -= CountMoves(pieces.PieceBitboards[1, 3].Value, pieces, PsLegalMoves.GenerateRookMask);
            queenMoves -= CountMoves(pieces.PieceBitboards[1, 4].Value, pieces, PsLegalMoves.GenerateQueenMask);

            mobility += Mobility.Knight * knightMoves;
            mobility += Mobility.Bishop * bishopMoves;
            mobility += Mobility.Rook * rookMoves;
            mobility += Mobility.Queen * queenMoves;
            return mobility;
        }

        private static int PawnStructureDoublePawn(Pieces pieces)
        {
            int doublePawnCounter = 0;
            for (int x = 0; x < 8; x++)
            {
                int whitePawns = BitOperations.PopCount(pieces.PieceBitboards[0, 0].Value & columns[x]);
                int blackPawns = BitOperations.PopCount(pieces.PieceBitboards[1, 0].Value & columns[x]);
                doublePawnCounter += Math.Max(0, whitePawns - 1) - Math.Max(0, blackPawns - 1);
            }
            return Pawns.DoublePawn * doublePawnCounter;
        }

        private static int PawnStructureConnectedPawn(Pieces pieces)
        {
            ulong whiteCaptures = PsLegalMoves.GeneratePawnLeftCapturesMask(pieces, PieceColor.White, true)
                                | PsLegalMoves.GeneratePawnRightCapturesMask(pieces, PieceColor.White, true);
            ulong blackCaptures = PsLegalMoves.GeneratePawnLeftCapturesMask(pieces, PieceColor.Black, true)
                                | PsLegalMoves.GeneratePawnRightCapturesMask(pieces, PieceColor.Black, true);

            int connectedPawnCounter = BitOperations.PopCount(whiteCaptures & pieces.PieceBitboards[0, 0].Value)
                                     - BitOperations.PopCount(blackCaptures & pieces.PieceBitboards[1, 0].Value);
            return Pawns.ConnectedPawn * connectedPawnCounter;
        }

        private static int PawnStructurePromotion(Pieces pieces)
        {
            int promotion = 0;
            ulong whitePawns = pieces.PieceBitboards[0, 0].Value;
            while (whitePawns != 0)
            {
                int index = BitOperations.TrailingZeroCount(whitePawns);
                whitePawns &= ~(1UL << index);
                promotion += (whitePassedPawnMasks[index] & pieces.PieceBitboards[1, 0].Value) != 0
                    ? Pawns.DefaultPawnPromotion[index / 8]
                    : Pawns.PassedPawnPromotion[index / 8];
            }

            ulong blackPawns = pieces.PieceBitboards[1, 0].Value;
            while (blackPawns != 0)
            {
                int index = BitOperations.TrailingZeroCount(blackPawns);
                blackPawns &= ~(1UL << index);
                promotion -= (blackPassedPawnMasks[index] & pieces.PieceBitboards[0, 0].Value) != 0
                    ? Pawns.DefaultPawnPromotion[7 - index / 8]
                    : Pawns.PassedPawnPromotion[7 - index / 8];
            }
            return promotion;
        }

        private static int KingSafetyCastling(bool whiteLong, bool whiteShort, bool blackLong, bool blackShort, bool whiteHappened, bool blackHappened)
        {
            int crashed = 0;
            if (!whiteHappened)
            {
                if (!whiteLong) crashed += Castlings.CrashedCastling;
                if (!whiteShort) crashed += Castlings.CrashedCastling;
            }
            if (!blackHappened)
            {
                if (!blackLong) crashed -= Castlings.CrashedCastling;
                if (!blackShort) crashed -= Castlings.CrashedCastling;
            }
            return crashed;
        }

        private static int KingSafetyPawnShield(Pieces pieces, bool whiteCastled, bool blackCastled)
        {
            int shield = 0;
            if (whiteCastled)
            {
                int kingPos = BitOperations.TrailingZeroCount(pieces.PieceBitboards[0, 5].Value);
                shield += BitOperations.PopCount(pieces.PieceBitboards[0, 0].Value & whitePawnShieldMasks[kingPos]);
            }
            if (blackCastled)
            {
                int kingPos = BitOperations.TrailingZeroCount(pieces.PieceBitboards[1, 5].Value);
                shield -= BitOperations.PopCount(pieces.PieceBitboards[1, 0].Value & blackPawnShieldMasks[kingPos]);
            }
            return Pawns.PawnShield * shield;
        }

        private static int TwoBishops(Pieces pieces)
        {
            int bonus = 0;
            if (BitOperations.PopCount(pieces.PieceBitboards[0, 2].Value) >= 2) bonus += Bishops.TwoBishops;
            if (BitOperations.PopCount(pieces.PieceBitboards[1, 2].Value) >= 2) bonus -= Bishops.TwoBishops;
            return bonus;
        }

        private static int EndgameEvaluation(Pieces pieces, bool whiteLeading)
        {
            if (BitOperations.PopCount(pieces.All.Value) > Endspiel.MaximumPiecesForEndspiel) return 0;

            int attackerSide = whiteLeading ? 0 : 1;
            int defenderSide = 1 - attackerSide;

            int attackerKing = BitOperations.TrailingZeroCount(pieces.PieceBitboards[attackerSide, 5].Value);
            int ax = attackerKing % 8, ay = attackerKing / 8;

            int defenderKing = BitOperations.TrailingZeroCount(pieces.PieceBitboards[defenderSide, 5].Value);
            int dx = defenderKing % 8, dy = defenderKing / 8;

            int evaluation = Endspiel.AttackerKingProximityToDefenderKing * (16 - Math.Abs(ax - dx) - Math.Abs(ay - dy))
                           + Endspiel.DistanceBetweenDefenderKingAndMiddle * (Math.Abs(dx - 3) + Math.Abs(dy - 4));

            return whiteLeading ? evaluation : -evaluation;
        }

        public static int Evaluate(Position position)
        {
            Pieces pieces = position.Pieces;
            bool whiteCastled = position.WhiteCastlingHappened;
            bool blackCastled = position.BlackCastlingHappened;

            int material = MaterialEvaluation(pieces);
            int mobility = MobilityEvaluation(pieces);
            int doublePawn = PawnStructureDoublePawn(pieces);
            int connectedPawn = PawnStructureConnectedPawn(pieces);
            int promotion = PawnStructurePromotion(pieces);
            int pawnShield = KingSafetyPawnShield(pieces, whiteCastled, blackCastled);
            int twoBishops = TwoBishops(pieces);
            int endgame = EndgameEvaluation(pieces, material >= 0);

            return material + mobility + doublePawn + connectedPawn + promotion + pawnShield + twoBishops + endgame;
        }
    }
}

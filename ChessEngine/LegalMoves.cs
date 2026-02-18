namespace ChessEngine
{
    using System;
    using System.Collections.Generic;

    public static class LegalMovesGenerator
    {
        public static MoveList Generate(Position position, PieceColor side, bool onlyCaptures)
        {
            var moves = new MoveList();
            var pieces = position.Pieces;

            // Генерация ходов пешками
            ulong pawnsLeftCaptures = PsLegalMoves.GeneratePawnLeftCapturesMask(pieces, side, false);
            ulong pawnsRightCaptures = PsLegalMoves.GeneratePawnRightCapturesMask(pieces, side, false);

            int pawnsLeftCaptureIndex = side == PieceColor.White ? -7 : 9;
            int pawnsRightCaptureIndex = side == PieceColor.White ? -9 : 7;

            PawnMaskToMoves(pieces, pawnsLeftCaptures, side, pawnsLeftCaptureIndex, true, MoveFlag.Default, moves);
            PawnMaskToMoves(pieces, pawnsRightCaptures, side, pawnsRightCaptureIndex, true, MoveFlag.Default, moves);

            if (!onlyCaptures)
            {
                ulong pawnsDefault = PsLegalMoves.GeneratePawnDefaultMask(pieces, side);
                ulong pawnsLong = PsLegalMoves.GeneratePawnLongMask(pieces, side);

                int pawnDefaultIndex = side == PieceColor.White ? -8 : 8;
                int pawnLongIndex = side == PieceColor.White ? -16 : 16;

                PawnMaskToMoves(pieces, pawnsDefault, side, pawnDefaultIndex, false, MoveFlag.Default, moves);
                PawnMaskToMoves(pieces, pawnsLong, side, pawnLongIndex, false, MoveFlag.PawnLongMove, moves);
            }

            // Генерация ходов другими фигурами
            GeneratePieceMoves(pieces, pieces.PieceBitboards[(int)side, (int)PieceType.Knight].Value,
                PieceType.Knight, side, PsLegalMoves.GenerateKnightMask, onlyCaptures, moves);
            GeneratePieceMoves(pieces, pieces.PieceBitboards[(int)side, (int)PieceType.Bishop].Value,
                PieceType.Bishop, side, PsLegalMoves.GenerateBishopMask, onlyCaptures, moves);
            GeneratePieceMoves(pieces, pieces.PieceBitboards[(int)side, (int)PieceType.Rook].Value,
                PieceType.Rook, side, PsLegalMoves.GenerateRookMask, onlyCaptures, moves);
            GeneratePieceMoves(pieces, pieces.PieceBitboards[(int)side, (int)PieceType.Queen].Value,
                PieceType.Queen, side, PsLegalMoves.GenerateQueenMask, onlyCaptures, moves);

            // Ходы короля
            int kingPos = Bitboard.FindMostSignificantBit(pieces.PieceBitboards[(int)side, (int)PieceType.King].Value);
            if (kingPos != -1)
            {
                ulong kingMask = PsLegalMoves.GenerateKingMask(pieces,(byte) kingPos, side, onlyCaptures);
                PieceMaskToMoves(pieces.Clone(), kingMask, (byte)kingPos, PieceType.King, side, moves);
            }

            // Специальные ходы
            AddEnPassantCaptures(pieces.Clone(), side, position.EnPassant, moves);
            if (!onlyCaptures)
                AddCastlingMoves(pieces.Clone(), side, position.CastlingAvailability, moves);

            return moves;
        }

        private static void GeneratePieceMoves(Pieces pieces, ulong pieceBitboard, PieceType pieceType,
            PieceColor side, Func<Pieces, byte, PieceColor, bool, ulong> maskGenerator,
            bool onlyCaptures, MoveList moves)
        {
            while (pieceBitboard != 0)
            {
                int attackerPos = Bitboard.FindMostSignificantBit(pieceBitboard);
                pieceBitboard &= ~(1UL << attackerPos);

                ulong mask = maskGenerator(pieces, (byte)attackerPos, side, onlyCaptures);
                PieceMaskToMoves(pieces.Clone(), mask, (byte)attackerPos, pieceType, side, moves);
            }
        }

        private static void PawnMaskToMoves(Pieces pieces, ulong mask, PieceColor attackerSide,
            int attackerIndexOffset, bool lookForDefender, MoveFlag flag, in MoveList moves)
        {
            while (mask != 0)
            {
                int targetSquare = Bitboard.FindMostSignificantBit(mask);
                mask &= ~(1UL << targetSquare);

                int fromSquare = targetSquare + attackerIndexOffset;
                PieceType defenderType = PieceType.None;

                if (lookForDefender)
                {
                    defenderType = GetDefenderType(pieces, targetSquare, Pieces.Inverse(attackerSide));
                }

                var move = new Move(
                    (byte)fromSquare,
                    (byte)targetSquare,
                    PieceType.Pawn,
                    attackerSide,
                    defenderType,
                    Pieces.Inverse(attackerSide),
                    flag
                );

                if (IsLegal(pieces.Clone(), move))
                {
                    if (targetSquare < 8 || targetSquare > 55) // Превращение пешки
                    {
                        foreach (MoveFlag promo in new[] { MoveFlag.PromoteToRook, MoveFlag.PromoteToQueen, MoveFlag.PromoteToKnight, MoveFlag.PromoteToBishop })
                        {
                            moves.PushBack(new Move((byte)(targetSquare + attackerIndexOffset), (byte)targetSquare, PieceType.Pawn, attackerSide, defenderType, Pieces.Inverse(attackerSide), promo));
                        }
                    }
                    else
                    {
                        moves.PushBack(move);
                    }
                }
            }
        }

        private static void PieceMaskToMoves(Pieces pieces, ulong mask, byte attackerPos,
            PieceType attackerType, PieceColor side, MoveList moves)
        {
            while (mask != 0)
            {
                int targetSquare = Bitboard.FindMostSignificantBit(mask);
                mask &= ~(1UL << targetSquare);

                PieceType defenderType = GetDefenderType(pieces, targetSquare, Pieces.Inverse(side));
                var move = new Move(
                    attackerPos,
                    (byte)targetSquare,
                    attackerType,
                    side,
                    defenderType,
                    Pieces.Inverse(side)
                );

                if (IsLegal(pieces, move))
                    moves.PushBack(move);
            }
        }

        private static PieceType GetDefenderType(Pieces pieces, int square, PieceColor defenderSide)
        {
            for (int i = 0; i < 6; i++)
                if ((pieces.PieceBitboards[(int)defenderSide, i].Value & (1UL << square)) != 0)
                    return (PieceType)i;

            return PieceType.None;
        }

        private static bool IsLegal(Pieces originalPieces, Move move)
        {
            var pieces = originalPieces.Clone();
            // Клонирование позиции и применение хода
            pieces.PieceBitboards[(int)move.AttackerSide, (int)move.AttackerType].Value &= ~(1UL << move.From);
            pieces.PieceBitboards[(int)move.AttackerSide, (int)move.AttackerType].Value |= 1UL << move.To;

            if (move.DefenderType != PieceType.None)
                pieces.PieceBitboards[(int)move.DefenderSide, (int)move.DefenderType].Value &= ~(1UL << move.To);

            // Обновление битбордов и проверка шаха
            pieces.UpdateBitboards();
            int kingPos = Bitboard.FindMostSignificantBit(
                pieces.PieceBitboards[(int)move.AttackerSide, (int)PieceType.King].Value
            );

            return !PsLegalMoves.IsSquareUnderAttack(
                pieces,
                (byte)kingPos,
                move.AttackerSide
            );
        }

        private static void AddEnPassantCaptures(Pieces pieces, PieceColor side, byte? enPassantSquare, MoveList moves)
        {
            if (enPassantSquare == null) return;

            int ep = enPassantSquare.Value;
            int[] offsets = side == PieceColor.White ? new[] { -7, -9 } : new[] { 7, 9 };

            foreach (int offset in offsets)
            {
                int fromSquare = ep + offset;
                if (fromSquare < 0 || fromSquare > 63) continue;

                if ((pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value & (1UL << fromSquare)) != 0)
                {
                    var move = new Move(
                        (byte)fromSquare,
                        (byte)ep,
                        PieceType.Pawn,
                        side,
                        PieceType.None,
                        PieceColor.None,
                        MoveFlag.EnPassantCapture
                    );

                    if (IsLegal(pieces, move))
                        moves.PushBack(move);
                }
            }
        }

        private static void AddCastlingMoves(Pieces pieces, PieceColor side, CastlingRights rights, MoveList moves)
        {
            int kingPos = Bitboard.FindMostSignificantBit(pieces.PieceBitboards[(int)side, (int)PieceType.King].Value);
            if (kingPos == -1) return;

            // Проверка длинной и короткой рокировки
            if (side == PieceColor.White)
            {
                if (rights.HasFlag(CastlingRights.WhiteLong) && CheckCastling(pieces, side, 0, 2, new[] { 2, 3 }))
                    moves.PushBack(CreateCastlingMove(side, 4, 2, MoveFlag.WhiteLongCastling));

                if (rights.HasFlag(CastlingRights.WhiteShort) && CheckCastling(pieces, side, 7, 6, new[] { 5, 6 }))
                    moves.PushBack(CreateCastlingMove(side, 4, 6, MoveFlag.WhiteShortCastling));
            }
            else
            {
                if (rights.HasFlag(CastlingRights.BlackLong) && CheckCastling(pieces, side, 56, 58, new[] { 58, 59 }))
                    moves.PushBack(CreateCastlingMove(side, 60, 58, MoveFlag.BlackLongCastling));

                if (rights.HasFlag(CastlingRights.BlackShort) && CheckCastling(pieces, side, 63, 62, new[] { 61, 62 }))
                    moves.PushBack(CreateCastlingMove(side, 60, 62, MoveFlag.BlackShortCastling));
            }
        }

        private static bool CheckCastling(Pieces pieces, PieceColor side, int rookPos, int kingTarget, int[] checkSquares)
        {
            // Проверка наличия ладьи и свободных клеток
            var kingPos = Bitboard.FindMostSignificantBit(pieces.PieceBitboards[(int)side, (int)PieceType.King].Value);
            if (PsLegalMoves.IsSquareUnderAttack(pieces, (byte)kingPos, side))
            {
                return false;
            }
            if ((pieces.PieceBitboards[(int)side, (int)PieceType.Rook].Value & (1UL << rookPos)) == 0)
                return false;

            foreach (int sq in checkSquares)
                if ((pieces.All.Value & (1UL << sq)) != 0 || PsLegalMoves.IsSquareUnderAttack(pieces, (byte)sq, side))
                    return false;



            return true;
        }

        private static Move CreateCastlingMove(PieceColor side, byte from, byte to, MoveFlag flag) =>
            new Move(from, to, PieceType.King, side, PieceType.None, PieceColor.None, flag);
    }
}

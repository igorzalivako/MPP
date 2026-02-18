using System.Numerics;

namespace ChessEngine
{

    public static class PsLegalMoves
    {
        public static ulong? GetMovesOrNull(bool returnNull, Pieces pieces, byte square, PieceColor side, bool onlyCaptures)
        {
            if (returnNull) return null;
            return GenerateQueenMask(pieces, square, side, onlyCaptures);
        }
        public static void ValidatePosition(Pieces? pieces, byte square, PieceColor side)
        {
            if (pieces is null)
                throw new ArgumentNullException(nameof(pieces), "Pieces object cannot be null");

            if (square >= 64)
                throw new ArgumentOutOfRangeException(nameof(square), "Square must be between 0 and 63");

            // Проверяем, что на указанном квадрате действительно есть фигура нужного цвета
            if ((pieces.SideBitboards[(int)side].Value & (1UL << square)) == 0)
                throw new InvalidOperationException($"No {side} piece on square {square}");
        }

        public static ulong GenerateKnightMask(Pieces pieces, byte p, PieceColor side, bool onlyCaptures)
        {
            if (onlyCaptures)
                return KnightMasks.Masks[p] & pieces.SideBitboards[(int)Pieces.Inverse(side)].Value;
            return KnightMasks.Masks[p] & pieces.InversionSideBitboards[(int)side].Value;
        }

        public static ulong GenerateKingMask(Pieces pieces, byte p, PieceColor side, bool onlyCaptures)
        {
            if (onlyCaptures)
                return KingMasks.Masks[p] & pieces.SideBitboards[(int)Pieces.Inverse(side)].Value;
            return KingMasks.Masks[p] & pieces.InversionSideBitboards[(int)side].Value;
        }

        private static ulong CalcRay(Pieces pieces, byte p, PieceColor side, bool onlyCaptures, SlidersMasks.Direction direction)
        {
            Bitboard mask = SlidersMasks.Masks[p, (int)direction];
            ulong blockers = mask.Value & pieces.All.Value;

            if (blockers == 0)
                return onlyCaptures ? 0 : mask.Value;

            int blockingSquare = direction switch
            {
                SlidersMasks.Direction.North or
                SlidersMasks.Direction.East or
                SlidersMasks.Direction.NorthEast or
                SlidersMasks.Direction.NorthWest
                    => BitOperations.TrailingZeroCount(blockers),

                _ => 63 - BitOperations.LeadingZeroCount(blockers)
            };

            Bitboard moves = new() { Value = onlyCaptures ? 0 : mask.Value ^ SlidersMasks.Masks[blockingSquare, (int)direction].Value };

            if ((pieces.SideBitboards[(int)side].Value & (1UL << blockingSquare)) != 0)
                moves.Value &= ~(1UL << blockingSquare);
            else
                moves.Value |= 1UL << blockingSquare;

            return moves.Value;
        }

        public static ulong GenerateBishopMask(Pieces pieces, byte p, PieceColor side, bool onlyCaptures)
        {
            ulong nw = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.NorthWest);
            ulong ne = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.NorthEast);
            ulong sw = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.SouthWest);
            ulong se = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.SouthEast);

            return nw | ne | sw | se;
        }

        public static ulong GenerateRookMask(Pieces pieces, byte p, PieceColor side, bool onlyCaptures)
        {
            ulong n = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.North);
            ulong s = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.South);
            ulong w = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.West);
            ulong e = CalcRay(pieces, p, side, onlyCaptures, SlidersMasks.Direction.East);

            return n | s | w | e;
        }

        public static ulong GenerateQueenMask(Pieces pieces, byte p, PieceColor side, bool onlyCaptures)
        {
            return GenerateBishopMask(pieces, p, side, onlyCaptures)
                 | GenerateRookMask(pieces, p, side, onlyCaptures);
        }

        public static ulong GeneratePawnDefaultMask(Pieces pieces, PieceColor side)
        {
            return side == PieceColor.White
                ? (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value << 8) & pieces.Empty.Value
                : (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value >> 8) & pieces.Empty.Value;
        }

        public static ulong GeneratePawnLongMask(Pieces pieces, PieceColor side)
        {
            ulong defaultMask = GeneratePawnDefaultMask(pieces, side);
            ulong rowMask = side == PieceColor.White ? Board.Rows[2].Value : Board.Rows[5].Value;

            return side == PieceColor.White
                ? ((defaultMask & rowMask) << 8) & pieces.Empty.Value
                : ((defaultMask & rowMask) >> 8) & pieces.Empty.Value;
        }

        public static ulong GeneratePawnLeftCapturesMask(Pieces pieces, PieceColor side, bool includeAllPossibleCaptures)
        {
            ulong mask = side == PieceColor.White
                ? (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value << 7) & Board.InversionColumns[7].Value
                : (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value >> 9) & Board.InversionColumns[7].Value;

            return includeAllPossibleCaptures
                ? mask
                : mask & pieces.SideBitboards[(int)Pieces.Inverse(side)].Value;
        }

        public static ulong GeneratePawnRightCapturesMask(Pieces pieces, PieceColor side, bool includeAllPossibleCaptures)
        {
            ulong mask = side == PieceColor.White
                ? (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value << 9) & Board.InversionColumns[0].Value
                : (pieces.PieceBitboards[(int)side, (int)PieceType.Pawn].Value >> 7) & Board.InversionColumns[0].Value;

            return includeAllPossibleCaptures
                ? mask
                : mask & pieces.SideBitboards[(int)Pieces.Inverse(side)].Value;
        }

        public static bool IsSquareUnderAttack(Pieces pieces, byte p, PieceColor side)
        {
            PieceColor opponent = Pieces.Inverse(side);
            ulong pawnAttacks = GeneratePawnLeftCapturesMask(pieces, opponent, true)
                              | GeneratePawnRightCapturesMask(pieces, opponent, true);

            return (pawnAttacks & (1UL << p)) != 0
                || (GenerateKnightMask(pieces, p, side, true) & pieces.PieceBitboards[(int)opponent, (int)PieceType.Knight].Value) != 0
                || (GenerateBishopMask(pieces, p, side, true) & pieces.PieceBitboards[(int)opponent, (int)PieceType.Bishop].Value) != 0
                || (GenerateRookMask(pieces, p, side, true) & pieces.PieceBitboards[(int)opponent, (int)PieceType.Rook].Value) != 0
                || (GenerateQueenMask(pieces, p, side, true) & pieces.PieceBitboards[(int)opponent, (int)PieceType.Queen].Value) != 0
                || (GenerateKingMask(pieces, p, side, true) & pieces.PieceBitboards[(int)opponent, (int)PieceType.King].Value) != 0;
        }

        private static byte AbsSubtract(byte left, byte right) =>
            left >= right ? (byte)(left - right) : (byte)(right - left);

        // Вспомогательные структуры
        public static class KnightMasks
        {
            public static readonly ulong[] Masks = new ulong[64];

            static KnightMasks()
            {
                for (byte x0 = 0; x0 < 8; x0++)
                {
                    for (byte y0 = 0; y0 < 8; y0++)
                    {
                        int index = y0 * 8 + x0;
                        Masks[index] = 0;

                        for (byte x1 = 0; x1 < 8; x1++)
                        {
                            for (byte y1 = 0; y1 < 8; y1++)
                            {
                                byte dx = AbsSubtract(x0, x1);
                                byte dy = AbsSubtract(y0, y1);

                                if ((dx == 2 && dy == 1) || (dx == 1 && dy == 2))
                                    Masks[index] |= 1UL << (y1 * 8 + x1);
                            }
                        }
                    }
                }
            }
        }
        public static class KingMasks 
        {
            public static readonly ulong[] Masks = new ulong[64];

            static KingMasks()
            {
                for (byte x0 = 0; x0 < 8; x0++)
                {
                    for (byte y0 = 0; y0 < 8; y0++)
                    {
                        int index = y0 * 8 + x0;
                        Masks[index] = 0;

                        for (byte x1 = 0; x1 < 8; x1++)
                        {
                            for (byte y1 = 0; y1 < 8; y1++)
                            {
                                byte dx = AbsSubtract(x0, x1);
                                byte dy = AbsSubtract(y0, y1);

                                if (dx <= 1 && dy <= 1)
                                    Masks[index] |= 1UL << (y1 * 8 + x1);
                            }
                        }
                    }
                }
            }
        }
        public static class SlidersMasks
        {
            public enum Direction : byte { North, South, West, East, NorthWest, NorthEast, SouthWest, SouthEast }
            public static readonly Bitboard[,] Masks = new Bitboard[64, 8];

            static SlidersMasks()
            {
                for (byte p = 0; p < 64; p++)
                {
                    for (Direction dir = 0; dir <= Direction.SouthEast; dir++)
                    {
                        int x = p % 8;
                        int y = p / 8;
                        Bitboard mask = default;

                        while (true)
                        {
                            switch (dir)
                            {
                                case Direction.North: y++; break;
                                case Direction.South: y--; break;
                                case Direction.West: x--; break;
                                case Direction.East: x++; break;
                                case Direction.NorthWest: y++; x--; break;
                                case Direction.NorthEast: y++; x++; break;
                                case Direction.SouthWest: y--; x--; break;
                                case Direction.SouthEast: y--; x++; break;
                            }

                            if (x < 0 || x > 7 || y < 0 || y > 7) break;
                            mask.Value |= 1UL << (y * 8 + x);
                        }

                        Masks[p, (int)dir] = mask;
                    }
                }
            }
        }
        public static class Board
        {
            public static readonly Bitboard[] Rows = CalcRows();
            public static readonly Bitboard[] Columns = CalcColumns();
            public static readonly Bitboard[] InversionColumns = CalcInversionColumns();
            private static Bitboard[] CalcRows()
            {
                Bitboard[] rows = new Bitboard[8];
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        rows[y].Value |= 1UL << (y * 8 + x);
                    }
                }
                return rows;
            }

            

            private static Bitboard[] CalcInversionColumns()
            {
                Bitboard[] inversionColumns = new Bitboard[8];
                for (int i = 0; i < 8; i++)
                {
                    inversionColumns[i].Value = ~Columns[i].Value;
                }
                return inversionColumns;
            }
            private static Bitboard[] CalcColumns()
            {
                Bitboard[] columns = new Bitboard[8];
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        columns[x].Value |= 1UL << (y * 8 + x);
                    }
                }
                return columns;
            }
        }

        // Вспомогательные методы для анализа позиции
        public static double EvaluatePosition(Pieces position)
        {
            // Простая материальная оценка
            double score = 0;

            // Веса фигур
            var pieceValues = new Dictionary<PieceType, double>
                {
                    { PieceType.Pawn, 1 },
                    { PieceType.Knight, 3 },
                    { PieceType.Bishop, 3 },
                    { PieceType.Rook, 5 },
                    { PieceType.Queen, 9 },
                    { PieceType.King, 0 } // Король не оценивается материально
                };

            foreach (PieceColor color in new[] { PieceColor.White, PieceColor.Black })
            {
                double multiplier = color == PieceColor.White ? 1 : -1;

                foreach (var kvp in pieceValues)
                {
                    int pieceCount = BitOperations.PopCount(position.PieceBitboards[(int)color, (int)kvp.Key].Value);
                    score += pieceCount * kvp.Value * multiplier;
                }
            }

            return score;
        }

        public static byte FindKingSquare(Pieces position, PieceColor color)
        {
            ulong kingBitboard = position.PieceBitboards[(int)color, (int)PieceType.King].Value;
            return (byte)BitOperations.TrailingZeroCount(kingBitboard);
        }

        public static int CalculateMobility(Pieces position, PieceColor color)
        {
            int totalMoves = 0;

            for (byte square = 0; square < 64; square++)
            {
                if ((position.SideBitboards[(int)color].Value & (1UL << square)) != 0)
                {
                    // Определяем тип фигуры на этом квадрате
                    PieceType pieceType = PieceType.Pawn; // По умолчанию
                    for (int i = 0; i < 6; i++)
                    {
                        if ((position.PieceBitboards[(int)color, i].Value & (1UL << square)) != 0)
                        {
                            pieceType = (PieceType)i;
                            break;
                        }
                    }

                    // Генерируем ходы в зависимости от типа фигуры
                    ulong moves = 0;
                    switch (pieceType)
                    {
                        case PieceType.Knight:
                            moves = GenerateKnightMask(position, square, color, false);
                            break;
                        case PieceType.Bishop:
                            moves = GenerateBishopMask(position, square, color, false);
                            break;
                        case PieceType.Rook:
                            moves = GenerateRookMask(position, square, color, false);
                            break;
                        case PieceType.Queen:
                            moves = GenerateQueenMask(position, square, color, false);
                            break;
                        case PieceType.King:
                            moves = GenerateKingMask(position, square, color, false);
                            break;
                        case PieceType.Pawn:
                            if (color == PieceColor.White)
                            {
                                moves = GeneratePawnDefaultMask(position, PieceColor.White);
                                moves |= GeneratePawnLongMask(position, PieceColor.White);
                                moves |= GeneratePawnLeftCapturesMask(position, PieceColor.White, false);
                                moves |= GeneratePawnRightCapturesMask(position, PieceColor.White, false);
                            }
                            else
                            {
                                moves = GeneratePawnDefaultMask(position, PieceColor.Black);
                                moves |= GeneratePawnLongMask(position, PieceColor.Black);
                                moves |= GeneratePawnLeftCapturesMask(position, PieceColor.Black, false);
                                moves |= GeneratePawnRightCapturesMask(position, PieceColor.Black, false);
                            }
                            break;
                    }

                    totalMoves += BitOperations.PopCount(moves);
                }
            }

            return totalMoves;
        }
    }
}

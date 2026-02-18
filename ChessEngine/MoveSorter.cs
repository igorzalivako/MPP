using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public static class MoveSorter
    {
        private static int EvaluateMove(Pieces pieces, Move move)
        {
            int evaluation = 0;

            // Штраф за перемещение фигуры под атаку пешек
            if (move.AttackerType != PieceType.Pawn)
            {
                PieceColor opponentSide = Pieces.Inverse(move.AttackerSide);
                ulong opponentPawnAttacks = PsLegalMoves.GeneratePawnLeftCapturesMask(pieces, opponentSide, true)
                                          | PsLegalMoves.GeneratePawnRightCapturesMask(pieces, opponentSide, true);

                if ((opponentPawnAttacks & (1UL << move.To)) != 0)
                {
                    evaluation -= move.AttackerType switch
                    {
                        PieceType.Knight => Material.Knight,
                        PieceType.Bishop => Material.Bishop,
                        PieceType.Rook => Material.Rook,
                        PieceType.Queen => Material.Queen,
                        _ => 0
                    };
                }
            }

            // Бонус за взятие фигур и штраф за потенциальную потерю
            if (move.DefenderType != PieceType.None)
            {
                evaluation += 1000 * (int)move.DefenderType switch
                {
                    (int)PieceType.Pawn => Material.Pawn,
                    (int)PieceType.Knight => Material.Knight,
                    (int)PieceType.Bishop => Material.Bishop,
                    (int)PieceType.Rook => Material.Rook,
                    (int)PieceType.Queen => Material.Queen,
                    _ => 0
                };

                evaluation -= (int)move.AttackerType switch
                {
                    (int)PieceType.Pawn => Material.Pawn,
                    (int)PieceType.Knight => Material.Knight,
                    (int)PieceType.Bishop => Material.Bishop,
                    (int)PieceType.Rook => Material.Rook,
                    (int)PieceType.Queen => Material.Queen,
                    _ => 0
                };
            }

            return evaluation;
        }

        public static void Sort(Pieces pieces, MoveList moves)
        {
            for (int i = 0; i < moves.Size - 1; i++)
            {
                for (int j = 0; j < moves.Size - i - 1; j++)
                {
                    int evalCurrent = EvaluateMove(pieces, moves[j]);
                    int evalNext = EvaluateMove(pieces, moves[j + 1]);

                    if (evalCurrent < evalNext)
                    {
                        // Меняем местами
                        var temp = moves[j];
                        moves[j] = moves[j + 1];
                        moves[j + 1] = temp;
                    }
                }
            }
        }
    }

    public static class Material
    {
        public const int Pawn = 100;
        public const int Knight = 300;
        public const int Bishop = 300;
        public const int Rook = 500;
        public const int Queen = 900;
    }
}

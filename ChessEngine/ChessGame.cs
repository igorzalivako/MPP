using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public class ChessGame
    {
        public Position Position { get; set; }
        public bool IsCheckmate { get => GetIsCheckmate(); }
        public bool IsStalemate { get => GetIsStalemate(); }

        public MoveList? AvailableMoves { get; set; }   

        public bool ApplyMove(Move move, PieceColor attackerSide)
        {
            MoveList availableMoves = LegalMovesGenerator.Generate(Position, attackerSide, false);
            Move? moveFromList;
            if ((moveFromList = availableMoves.FirstOrDefault((Move m) => m.From == move.From && m.To == move.To && CompareFlags(m.Flag, move.Flag))) != null)
            {
                Position.MakeMove(moveFromList.Value);
                AvailableMoves = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CompareFlags(MoveFlag flag1, MoveFlag flag2)
        {
            if (flag2 == MoveFlag.Default)
            {
                return true;
            }
            else
            {
                return flag1 == flag2;
            }
        }

        public MoveList GetValidMoves(PieceColor attackerSide)
        {
            MoveList availableMoves = LegalMovesGenerator.Generate(Position, attackerSide, false);
            AvailableMoves = availableMoves;
            // lifted operator
            return availableMoves;
        }

        public PieceColor InverseColor(PieceColor callerColor)
        {
            if (callerColor == PieceColor.White)
            {
                return PieceColor.Black;
            }
            else
            {
                return PieceColor.White;
            }
        }

        // Реализовать логику движка
        public void LoadPosition(Position position)
        {
            Position = position;
            AvailableMoves = null;
        }

        private bool GetIsCheckmate()
        {
            bool isCheckmate = false;

            UpdateAvailableMoves();

            if (AvailableMoves.Size == 0)
            {
                if (PsLegalMoves.IsSquareUnderAttack(Position.Pieces, Bitboard.FindMostSignificantBit(Position.Pieces.PieceBitboards[(int)Position.ActiveColor, (int)PieceType.King].Value), Position.ActiveColor))
                {
                    isCheckmate = true;
                }
            }
            return isCheckmate;
        }

        private bool GetIsStalemate()
        {
            bool isStalemate = false;

            UpdateAvailableMoves();

            if (AvailableMoves.Size == 0 && !PsLegalMoves.IsSquareUnderAttack(Position.Pieces, Bitboard.FindMostSignificantBit(Position.Pieces.PieceBitboards[(int)Position.ActiveColor, (int)PieceType.King].Value), Position.ActiveColor)
                || Position.RepetitionHistory.GetRepetitionNumber(Position.Hash) >= 3
                || Position.FiftyMovesCounter >= 50)
            {
                isStalemate = true;
            }
            return isStalemate;
        }

        private void UpdateAvailableMoves()
        {
            if (AvailableMoves == null)
            {
                AvailableMoves = LegalMovesGenerator.Generate(Position, Position.ActiveColor, false);
            }
        }

        public string GetFen()
        {
            StringBuilder fen = new StringBuilder();
            int freeSquaresCount = 0;
            for (int j = 7; j >= 0; j--)
            {
                for (int i = 0; i < 8; i++)

                {
                    PieceType currentPiece = PieceType.None;
                    PieceColor currentPieceColor = PieceColor.None;
                    // проверяем все фигуры на данной позиции
                    for (PieceType pieceType = PieceType.Pawn; pieceType < PieceType.None; pieceType++)
                    {
                        for (PieceColor pieceColor = 0; pieceColor < PieceColor.None; pieceColor++)
                        {
                            if ((Position.Pieces.PieceBitboards[(int)pieceColor, (int)pieceType].Value & (1ul << GetBitboardPosition(i, j))) != 0)
                            {
                                currentPiece = pieceType;
                                currentPieceColor = pieceColor;
                                break;
                            }
                        }
                        if (currentPiece != PieceType.None)
                        {
                            break;
                        }
                    }
                    if (currentPiece == PieceType.None)
                    {
                        freeSquaresCount++;
                    }
                    else
                    {
                        if (freeSquaresCount > 0)
                        {
                            fen.Append(freeSquaresCount.ToString());
                            freeSquaresCount = 0;
                        }
                        char piece = currentPiece switch
                        {
                            PieceType.Pawn => currentPieceColor switch { PieceColor.White => 'P', PieceColor.Black => 'p' },
                            PieceType.Knight => currentPieceColor switch { PieceColor.White => 'N', PieceColor.Black => 'n' },
                            PieceType.Rook => currentPieceColor switch { PieceColor.White => 'R', PieceColor.Black => 'r' },
                            PieceType.Bishop => currentPieceColor switch { PieceColor.White => 'B', PieceColor.Black => 'b' },
                            PieceType.Queen => currentPieceColor switch { PieceColor.White => 'Q', PieceColor.Black => 'q' },
                            PieceType.King => currentPieceColor switch { PieceColor.White => 'K', PieceColor.Black => 'k' }
                        };
                        fen.Append(piece);
                    }
                }
                if (freeSquaresCount > 0)
                {
                    fen.Append(freeSquaresCount.ToString());
                    freeSquaresCount = 0;
                }
                fen.Append('/');
            }

            return fen.Remove(fen.Length - 1, 1).ToString();
        }

        private int GetBitboardPosition(int x, int y) => y * 8 + x;
    }
}

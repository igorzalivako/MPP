using ChessEngine;

namespace EngineTest;

public class Program
{
    public static void Main()
    {
        AI ai = new AI("D:\\Курсовая 2 курс 1 семестр\\Debug\\Chess Master\\Chess Master\\Текст.txt");
        Position testGame = new Position("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
        string moveString;
        byte from, to;
        Move nextPlayerMove, bestAIMove;
        MoveList possibleMoves;
        bool moveFound, isMate = false;
        nextPlayerMove.AttackerSide = PieceColor.White;
        while (!isMate)
        {
            do
            {
                moveString = Console.ReadLine();
                from = (byte)((moveString[1] - '1') * 8 + moveString[0] - 'a');
                to = (byte)((moveString[3] - '1') * 8 + moveString[2] - 'a');

                possibleMoves = LegalMovesGenerator.Generate(testGame, testGame.MoveCounter - Math.Floor(testGame.MoveCounter) < 1e-7 ? PieceColor.White : PieceColor.Black, false);
                if (possibleMoves.Size == 0)
                {
                    isMate = true;
                }
                moveFound = false;

                Move? move = possibleMoves.FirstOrDefault((Move m) => m.From == from && m.To == to);
                if (move.HasValue)
                {
                    testGame.MakeMove(move.Value);
                    moveFound = true;
                    possibleMoves = LegalMovesGenerator.Generate(testGame, testGame.MoveCounter - Math.Floor(testGame.MoveCounter) < 1e-7 ? PieceColor.White : PieceColor.Black, false);
                    bestAIMove = ai.FindBestMove(testGame, PieceColor.Black, 0, 2000);
                    possibleMoves = LegalMovesGenerator.Generate(testGame, testGame.MoveCounter - Math.Floor(testGame.MoveCounter) < 1e-7 ? PieceColor.White : PieceColor.Black, false);
                    MoveSorter.Sort(testGame.Pieces, possibleMoves);
                    Console.WriteLine($"{(char)(bestAIMove.From % 8 + 'a')}" + $"{bestAIMove.From / 8 + 1}" + $"{(char)(bestAIMove.To % 8 + 'a')}" + $"{bestAIMove.To / 8 + 1}\n");
                    testGame.MakeMove(bestAIMove);
                }
                else
                {
                    Console.WriteLine("Ход некорректный, введите другой!");
                }

            } while (!moveFound);
        }
    }
}

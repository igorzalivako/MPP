using System.Diagnostics;

namespace ChessEngine
{
    public class AI
    {
        const int POSITIVE_INFINITY = +1_000_000_000;
        const int NEGATIVE_INFINITY = -1_000_000_000;

        private static volatile bool _stopSearch;
        private readonly OpeningBook _openingBook;
        private int _evaluated;
        private int _maximalDepth;
        private int _ttCutoffs;

        public AI(string openingBookPath)
        {
            _openingBook = new OpeningBook(openingBookPath);
        }

        public AI(string openingBookContent, bool isContent)
        {
            _openingBook = new OpeningBook(openingBookContent, isContent);
        }

        public Move FindBestMove(Position position, PieceColor side, int minMs, int maxMs)
        {
            StaticEvaluator.Evaluate(position);

            long timeStart = Stopwatch.GetTimestamp();
            _stopSearch = false;
            var tt = new TranspositionTable();

            var openingBookResult = _openingBook.TryFindMove(position);
            if (openingBookResult.Item2 != 0)
                return openingBookResult.Item1;

            int bestMoveEvaluation;
            Move bestMove = new Move();
            Task<Tuple<int, Move>> bestMoveThread = null;
            bool updateBestMove;

            for (int i = 1; i < 1_000; i++)
            {
                _evaluated = 0;
                _maximalDepth = 0;
                _ttCutoffs = 0;

                bestMoveThread = Task.Run(() => _BestMove(position, side, i, tt));

                updateBestMove = true;

                while (!bestMoveThread.IsCompleted)
                {
                    long elapsedMs = (Stopwatch.GetTimestamp() - timeStart) * 1_000 / Stopwatch.Frequency;
                    if (elapsedMs >= maxMs)
                    {
                        updateBestMove = false;
                        break;
                    }
                    Thread.Sleep(200);
                }

                if (updateBestMove || i == 1)
                {
                    var result = bestMoveThread.Result;
                    bestMoveEvaluation = result.Item1;
                    bestMove = result.Item2;
                }
                else
                {
                    _stopSearch = true;
                    break;
                }

                if (bestMoveEvaluation > POSITIVE_INFINITY || bestMoveEvaluation < NEGATIVE_INFINITY)
                    break;
            }

            long elapsedTotalMs = (Stopwatch.GetTimestamp() - timeStart) * 1_000_000 / Stopwatch.Frequency;
            int remainingMs = (int)Math.Max(0, minMs - elapsedTotalMs);
            Thread.Sleep(remainingMs);

            return bestMove;
        }

        private Tuple<int, Move> _BestMove(Position position, PieceColor side, int depth, TranspositionTable tt)
        {
            return side == PieceColor.White
                ? AlphaBetaMax(position, NEGATIVE_INFINITY, POSITIVE_INFINITY, depth, 0, tt)
                : AlphaBetaMin(position, NEGATIVE_INFINITY, POSITIVE_INFINITY, depth, 0, tt);
        }

        private Tuple<int, Move> AlphaBetaMin(Position position, int alpha, int beta, int depthLeft, int depthCurrent, TranspositionTable tt)
        {
            Position positionCopy = position.Clone();
            if (_stopSearch) return Tuple.Create(0, new Move());
            if (depthCurrent > _maximalDepth) _maximalDepth = depthCurrent;

            if (depthLeft == 0)
                return Tuple.Create(AlphaBetaMinOnlyCaptures(positionCopy, alpha, beta, depthCurrent), new Move());

            if (positionCopy.FiftyMovesCounter >= 50 || positionCopy.RepetitionHistory.GetRepetitionNumber(positionCopy.Hash) >= 3)
                return Tuple.Create(0, new Move());

            var moves = LegalMovesGenerator.Generate(positionCopy, PieceColor.Black, false);
            MoveSorter.Sort(positionCopy.Pieces, moves);
            Move bestMove = new Move();
            byte bestMoveIndex = 0;

            bool inCheck = PsLegalMoves.IsSquareUnderAttack(
                positionCopy.Pieces,
                Bitboard.FindMostSignificantBit(positionCopy.Pieces.PieceBitboards[(int)PieceColor.Black, (int)PieceType.King].Value),
                PieceColor.Black
            );

            if (moves.Size == 0)
                return inCheck
                    ? Tuple.Create(POSITIVE_INFINITY - depthCurrent, new Move())
                    : Tuple.Create(0, new Move());

            Position copy;
            byte ttResult = tt.TryToFindBestMoveIndex(positionCopy.Hash.Hash);

            for (byte i = 0; i < moves.Size; i++)
            {
                Move move;
                if (ttResult >= moves.Size)
                    move = moves[i];
                else
                {
                    if (i == 0)
                        move = moves[ttResult];
                    else
                        move = i == ttResult ? moves[0] : moves[i];
                }

                copy = positionCopy.Clone();
                copy.MakeMove(move);
                int evaluation = AlphaBetaMax(copy, alpha, beta, depthLeft - (inCheck ? 0 : 1), depthCurrent + 1, tt).Item1;

                if (evaluation <= alpha)
                {
                    if (ttResult >= moves.Size || i != 0)
                        tt.AddEntry(new Entry() { Hash = positionCopy.Hash.Hash, Depth = depthLeft, BestMoveIndex = bestMoveIndex });
                    else
                        _ttCutoffs++;
                    return Tuple.Create(alpha, bestMove);
                }

                if (evaluation < beta)
                {
                    bestMove = move;
                    bestMoveIndex = i;
                    beta = evaluation;
                }
            }

            tt.AddEntry(new Entry { Hash = positionCopy.Hash.Hash, Depth = depthLeft, BestMoveIndex = bestMoveIndex });
            return Tuple.Create(beta, bestMove);
        }

        private Tuple<int, Move> AlphaBetaMax(Position position, int alpha, int beta, int depthLeft, int depthCurrent, TranspositionTable tt)
        {
            Position positionCopy = position.Clone();
            if (_stopSearch) return Tuple.Create(0, new Move());
            if (depthCurrent > _maximalDepth) _maximalDepth = depthCurrent;

            if (depthLeft == 0)
                return Tuple.Create(AlphaBetaMaxOnlyCaptures(positionCopy, alpha, beta, depthCurrent), new Move());

            if (positionCopy.FiftyMovesCounter >= 50 || positionCopy.RepetitionHistory.GetRepetitionNumber(positionCopy.Hash) >= 3)
                return Tuple.Create(0, new Move());

            var moves = LegalMovesGenerator.Generate(positionCopy, PieceColor.White, false);
            MoveSorter.Sort(positionCopy.Pieces, moves);
            Move bestMove = new Move();
            byte bestMoveIndex = 0;

            bool inCheck = PsLegalMoves.IsSquareUnderAttack(
                positionCopy.Pieces,
                Bitboard.FindMostSignificantBit(positionCopy.Pieces.PieceBitboards[(int)PieceColor.White, (int)PieceType.King].Value),
                PieceColor.White
            );

            if (moves.Size == 0)
                return inCheck
                    ? Tuple.Create(NEGATIVE_INFINITY + depthCurrent, new Move())
                    : Tuple.Create(0, new Move());

            byte ttResult = tt.TryToFindBestMoveIndex(positionCopy.Hash.Hash);
            Position copy;

            for (byte i = 0; i < moves.Size; i++)
            {
                Move move;
                if (ttResult >= moves.Size)
                    move = moves[i];
                else
                {
                    if (i == 0)
                        move = moves[ttResult];
                    else
                        move = i == ttResult ? moves[0] : moves[i];
                }

                copy = positionCopy.Clone();
                copy.MakeMove(move);
                int evaluation = AlphaBetaMin(copy, alpha, beta, depthLeft - (inCheck ? 0 : 1), depthCurrent + 1, tt).Item1;

                if (evaluation >= beta)
                {
                    if (ttResult >= moves.Size || i != 0)
                        tt.AddEntry(new Entry { Hash = positionCopy.Hash.Hash, Depth = depthLeft, BestMoveIndex = bestMoveIndex });
                    else
                        _ttCutoffs++;
                    return Tuple.Create(beta, bestMove);
                }

                if (evaluation > alpha)
                {
                    bestMove = move;
                    bestMoveIndex = i;
                    alpha = evaluation;
                }
            }

            tt.AddEntry(new Entry { Hash = positionCopy.Hash.Hash, Depth = depthLeft, BestMoveIndex = bestMoveIndex });
            return Tuple.Create(alpha, bestMove);
        }

        private int AlphaBetaMinOnlyCaptures(Position position, int alpha, int beta, int depthCurrent)
        {
            Position positionCopy = position.Clone();
            if (_stopSearch) return 0;
            if (depthCurrent > _maximalDepth) _maximalDepth = depthCurrent;

            int evaluation = StaticEvaluator.Evaluate(positionCopy);
            _evaluated++;

            if (evaluation <= alpha) return alpha;
            if (evaluation < beta) beta = evaluation;

            var moves = LegalMovesGenerator.Generate(positionCopy, PieceColor.Black, true);
            MoveSorter.Sort(positionCopy.Pieces, moves);

            for (int i = 0; i < moves.Size; i++)
            {
                var move = moves[i];
                var copy = positionCopy.Clone();
                copy.MakeMove(move);
                evaluation = AlphaBetaMaxOnlyCaptures(copy, alpha, beta, depthCurrent + 1);

                if (evaluation <= alpha) return alpha;
                if (evaluation < beta) beta = evaluation;
            }
            return beta;
        }

        private int AlphaBetaMaxOnlyCaptures(Position position, int alpha, int beta, int depthCurrent)
        {
            Position positionCopy = position.Clone();
            if (_stopSearch) return 0;
            if (depthCurrent > _maximalDepth) _maximalDepth = depthCurrent;

            int evaluation = StaticEvaluator.Evaluate(positionCopy);
            _evaluated++;

            if (evaluation >= beta) return beta;
            if (evaluation > alpha) alpha = evaluation;

            var moves = LegalMovesGenerator.Generate(positionCopy, PieceColor.White, true);
            MoveSorter.Sort(positionCopy.Pieces, moves);

            for (int i = 0; i < moves.Size; i++)
            {
                var move = moves[i];
                var copy = positionCopy.Clone();
                copy.MakeMove(move);
                evaluation = AlphaBetaMinOnlyCaptures(copy, alpha, beta, depthCurrent + 1);

                if (evaluation >= beta) return beta;
                if (evaluation > alpha) alpha = evaluation;
            }
            return alpha;
        }
    }
}

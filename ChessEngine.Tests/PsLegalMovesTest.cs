using System.Numerics;
using TestFrameworkCore.Assertions;
using TestFrameworkCore.Attributes;
using static ChessEngine.PsLegalMoves;
using static ChessEngine.PsLegalMoves.SlidersMasks;


namespace ChessEngine.Tests
{
    namespace ChessEngineTests
    {
        [TestClass(Category = "BishopMoves", Priority = 1)]
        public class BishopMoveTests
        {
            [TestMethod]
            [TestCase((byte)35, 13, Name = "Center e4")]      // e4 - центр
            [TestCase((byte)7, 7, Name = "Corner a1")]        // a1 - угол
            [TestCase((byte)27, 13, Name = "Center e5")]      // e5 - центр
            [TestCase((byte)34, 11, Name = "Edge c5")]        // c5 - край
            public void BishopMoves_FromDifferentSquares_ReturnsCorrectMoveCount(byte square, int expectedMoveCount)
            {
                // Arrange
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong moves = GenerateBishopMask(emptyBoard, square, PieceColor.White, false);
                int actualMoveCount = BitOperations.PopCount(moves);

                // Assert
                Assert.AreEqual(expectedMoveCount, actualMoveCount,
                    $"Bishop from square {square} should have {expectedMoveCount} moves, but had {actualMoveCount}");

                // Дополнительная проверка: слон не может ходить на клетку, на которой стоит
                Assert.IsFalse((moves & (1UL << square)) != 0, "Bishop should not be able to move to its own square");
            }

            [TestMethod]
            public void BishopMoves_FromCenterEmptyBoard_Returns13Moves()
            {
                // Arrange
                byte e4 = 36;
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong moves = PsLegalMoves.GenerateBishopMask(emptyBoard, e4, PieceColor.White, false);

                // Assert
                // Слон на e4 должен иметь 13 ходов по диагоналям
                int moveCount = BitOperations.PopCount(moves);
                Assert.AreEqual(13, moveCount, "Bishop from center should have 13 moves on empty board");

                Assert.IsTrue((moves & (1UL << 45)) != 0, "Should include f5");
                Assert.IsTrue((moves & (1UL << 54)) != 0, "Should include g6");
                Assert.IsTrue((moves & (1UL << 63)) != 0, "Should include h7");
                Assert.IsTrue((moves & (1UL << 27)) != 0, "Should include d5");
                Assert.IsTrue((moves & (1UL << 18)) != 0, "Should include c6");
            }

            [TestMethod]
            public void BishopMoves_BlockedByOwnPiece_StopsBeforeBlock()
            {
                // Arrange
                byte e4 = 36;
                var pieces = TestPositionBuilder.CreatePositionWithPiece(45, PieceColor.White, PieceType.Pawn); // f5 занят своей пешкой

                // Act
                ulong moves = GenerateBishopMask(pieces, e4, PieceColor.White, false);

                // Assert
                // Не должен включать f5 (так как на f5 своя пешка, и нельзя пройти дальше)
                Assert.IsTrue((moves & (1UL << 45)) == 0, "Should not include f5 (blocking square)");
                // Не должен включать квадраты за f5 (g6, h7)
                Assert.IsTrue((moves & (1UL << 54)) == 0, "Should not include g6 (behind own piece)");
                Assert.IsTrue((moves & (1UL << 63)) == 0, "Should not include h7 (behind own piece)");
            }

            [TestMethod]
            public void BishopMoves_BlockedByEnemyPiece_IncludesEnemySquare()
            {
                // Arrange
                byte e4 = 36;
                var pieces = TestPositionBuilder.CreatePositionWithPiece(45, PieceColor.Black, PieceType.Pawn); // f5 с вражеской пешкой

                // Act
                ulong moves = PsLegalMoves.GenerateBishopMask(pieces, e4, PieceColor.White, false);

                // Assert
                Assert.IsTrue((moves & (1UL << 45)) != 0, "Should include f5 (enemy square)");
                Assert.IsFalse((moves & (1UL << 54)) != 0, "Should not include g6 (behind enemy piece)");
            }

            [TestMethod]
            public void BishopMoves_OnlyCaptures_ReturnsOnlyEnemySquares()
            {
                // Arrange
                byte e4 = 36;
                var pieces = TestPositionBuilder.CreatePositionWithPieces(new Dictionary<byte, (PieceColor, PieceType)>
                {
                    { 45, (PieceColor.Black, PieceType.Pawn) }, // f5 - враг
                    { 54, (PieceColor.White, PieceType.Pawn) }, // g6 - свой
                });

                // Act
                ulong captures = PsLegalMoves.GenerateBishopMask(pieces, e4, PieceColor.White, true);

                // Assert
                Assert.AreEqual(1UL << 45, captures, "Should only capture on f5");
                Assert.IsFalse((captures & (1UL << 54)) != 0, "Should not include own piece on g6");
            }
        }

        [TestClass(Category = "RookMoves", Priority = 1)]
        public class RookMoveTests
        {

            [TestMethod]
            [TestCase((byte)36, (byte)44, (byte)52, Name = "Rook blocked to the north")]
            [TestCase((byte)36, (byte)28, (byte)20, Name = "Rook blocked to the south")]
            [TestCase((byte)36, (byte)37, (byte)38, Name = "Rook blocked to the east")]
            [TestCase((byte)36, (byte)35, (byte)34, Name = "Rook blocked to the west")]
            public void RookMoves_WithBlocker_StopsCorrectly(
                byte rookSquare,
                byte blockerSquare,
                byte beyondBlocker)
            {
                // Arrange
                var pieces = TestPositionBuilder.CreatePositionWithPieces(new Dictionary<byte, (PieceColor, PieceType)>
                {
                    { rookSquare, (PieceColor.White, PieceType.Rook) },
                    { blockerSquare, (PieceColor.Black, PieceType.Pawn) } // Враг на пути
                });

                // Act
                ulong moves = GenerateRookMask(pieces, rookSquare, PieceColor.White, false);

                // Assert
                // Должен включать квадрат с врагом (можно взять)
                Assert.IsTrue((moves & (1UL << blockerSquare)) != 0,
                    $"Should include blocker square {blockerSquare} (enemy)");

                // Не должен включать квадраты за врагом
                Assert.IsFalse((moves & (1UL << beyondBlocker)) != 0,
                    $"Should not include square {beyondBlocker} (beyond blocker)");
            }

            [TestMethod]
            public void RookMoves_BlockedByPieces_StopsCorrectly()
            {
                // Arrange
                byte e4 = 36;
                var pieces = TestPositionBuilder.CreatePositionWithPieces(new Dictionary<byte, (PieceColor, PieceType)>
                {
                    { 44, (PieceColor.Black, PieceType.Pawn) }, // e5 - вражеская пешка на пути на север
                    { 28, (PieceColor.White, PieceType.Pawn) }, // e3 - своя пешка на пути на юг
                });

                // Act
                ulong moves = PsLegalMoves.GenerateRookMask(pieces, e4, PieceColor.White, false);

                // Assert
                // На север должен включать e5 (враг), но не e6
                Assert.IsTrue((moves & (1UL << 44)) != 0, "Should include e5 (enemy)");
                Assert.IsFalse((moves & (1UL << 52)) != 0, "Should not include e6 (beyond enemy)");

                // На юг не должен включать e3 (своя) и e2
                Assert.IsTrue((moves & (1UL << 28)) == 0, "Should not include e3 (own piece as blocker)");
                Assert.IsFalse((moves & (1UL << 20)) != 0, "Should not include e2 (beyond own piece)");
            }
        }

        [TestClass(Category = "QueenMoves", Priority = 1)]
        public class QueenMoveTests
        {
            [TestMethod]
            public void QueenMoves_IsCombinationOfBishopAndRook()
            {
                // Arrange
                byte e4 = 36;
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong queenMoves = GenerateQueenMask(emptyBoard, e4, PieceColor.White, false);
                ulong bishopMoves = GenerateBishopMask(emptyBoard, e4, PieceColor.White, false);
                ulong rookMoves = GenerateRookMask(emptyBoard, e4, PieceColor.White, false);

                // Assert
                Assert.AreEqual(bishopMoves | rookMoves, queenMoves,
                    "Queen moves should be union of bishop and rook moves");
            }
        }

        [TestClass(Category = "AdvancedScenarios", Priority = 2)]
        public class AdvancedScenarioTests
        {
            [TestMethod]
            public void Test_QueenMoves_WithMultipleBlockers_ComplexAssertions()
            {
                byte d4 = 27;
                var pieces = TestPositionBuilder.CreatePositionWithPieces(new Dictionary<byte, (PieceColor, PieceType)>
                {
                    // Свои фигуры (белые)
                    { d4, (PieceColor.White, PieceType.Queen) },
                    { 19, (PieceColor.White, PieceType.Pawn) },  // d3 - своя пешка (юг)
                    { 29, (PieceColor.White, PieceType.Pawn) },  // e4 - своя пешка (восток)
                    { 26, (PieceColor.White, PieceType.Pawn) },  // c4 - своя пешка (запад)
                
                    // Чужие фигуры (черные)
                    { 43, (PieceColor.Black, PieceType.Pawn) },  // d6 - враг (север)
                    { 45, (PieceColor.Black, PieceType.Pawn) },  // f5 - враг (северо-восток)
                });

                // Act
                ulong queenMoves = GenerateQueenMask(pieces, d4, PieceColor.White, false);
                ulong queenCaptures = GenerateQueenMask(pieces, d4, PieceColor.White, true);

                var moveList = new List<byte>();
                var captureList = new List<byte>();

                for (byte i = 0; i < 64; i++)
                {
                    if ((queenMoves & (1UL << i)) != 0)
                        moveList.Add(i);
                    if ((queenCaptures & (1UL << i)) != 0)
                        captureList.Add(i);
                }

                Assert.IsTrue(queenMoves != 0, "Queen should have moves");
                Assert.IsFalse(queenMoves == 0, "Queen moves should not be zero");

                Assert.AreNotEqual(queenMoves, queenCaptures, "All moves and captures should differ");
                Assert.AreEqual(2, captureList.Count, "Should have exactly 2 captures");

                Assert.IsNotNull(moveList, "Move list should not be null");
                Assert.IsNotNull(captureList, "Capture list should not be null");

                Assert.Contains<byte>(moveList, 43, "Should contain d6 (enemy - capture)");
                Assert.Contains<byte>(moveList, 45, "Should contain f5 (enemy - capture)");

                // Не должны содержать квадраты за блокирующими фигурами
                Assert.IsFalse(moveList.Contains(11), "Should not contain d2 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(21), "Should not contain f4 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(25), "Should not contain b4 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(51), "Should not contain d7 (behind enemy)");

                Assert.InRange(moveList.Count, 0, 27, "Move count should be in reasonable range");
                Assert.InRange(captureList.Count, 0, 8, "Capture count should be in reasonable range");

                Assert.Throws<InvalidOperationException>(() => ValidatePosition(pieces, 24, PieceColor.White),
                    "Should throw for invalid square");
                Assert.DoesNotThrow(
                    () => ValidatePosition(pieces, 27, PieceColor.White),
                    "Should throw for invalid square");
            }
        }

        [TestClass(Category = "PawnMoves", Priority = 1)]
        public class PawnMoveTests
        {
            private Pieces? _testPosition;
            private PieceColor _currentSide;
            private byte _pawnSquare;

            [BeforeEach]
            public void Setup()
            {
                _pawnSquare = 12; // e2
                _testPosition = TestPositionBuilder.CreatePositionWithPiece(_pawnSquare, PieceColor.White, PieceType.Pawn);
                _currentSide = PieceColor.White;
            }

            [AfterEach]
            public void Cleanup()
            {
                _testPosition = null;
            }

            [TestMethod]
            public void PawnMoves_WhenBlocked_NoForwardMoves()
            {
                // Добавляем черную пешку на e3, блокирующую ход белой пешки
                _testPosition!.PieceBitboards[(int)PieceColor.Black, (int)PieceType.Pawn].Value |= 1UL << 20;
                _testPosition.UpdateBitboards();

                // Act
                ulong moves = GeneratePawnDefaultMask(_testPosition, _currentSide);
                ulong longMoves = GeneratePawnLongMask(_testPosition, _currentSide);

                // Assert
                Assert.AreEqual(0UL, moves, "Pawn should have no forward moves when blocked");
                Assert.AreEqual(0UL, longMoves, "Pawn should have no long moves when forward square is occupied");
            }

            [TestMethod]
            public void PawnCaptures_WhenEnemyPiecesExist_CanCapture()
            {
                // Черные пешки на d3 (19) и f3 (21)
                _testPosition!.PieceBitboards[(int)PieceColor.Black, (int)PieceType.Pawn].Value |= 1UL << 19; // d3
                _testPosition.PieceBitboards[(int)PieceColor.Black, (int)PieceType.Pawn].Value |= 1UL << 21; // f3
                _testPosition.UpdateBitboards();

                // Act
                ulong leftCaptures = GeneratePawnLeftCapturesMask(_testPosition, _currentSide, false);
                ulong rightCaptures = GeneratePawnRightCapturesMask(_testPosition, _currentSide, false);
                ulong allCaptures = leftCaptures | rightCaptures;

                // Assert
                Assert.AreEqual(2, BitOperations.PopCount(allCaptures), "Should have 2 captures when enemies are present");
                Assert.IsTrue((allCaptures & (1UL << 19)) != 0, "Should capture on d3");
                Assert.IsTrue((allCaptures & (1UL << 21)) != 0, "Should capture on f3");
            }

            [TestMethod]
            public void PawnMoves_FromNonStartingRank_NoLongMove()
            {
                // Act
                ulong longMoves = GeneratePawnLongMask(_testPosition!, _currentSide);
                ulong normalMoves = GeneratePawnDefaultMask(_testPosition!, _currentSide);

                // Assert
                Assert.AreEqual(268435456ul, longMoves, "Pawn not on starting rank should have a long move");
                Assert.IsTrue(normalMoves != 0, "Pawn should still have normal one-square move");
                Assert.AreEqual(1UL << 20, normalMoves, "Pawn on e2 should move to e4");
            }
        }
    }
}

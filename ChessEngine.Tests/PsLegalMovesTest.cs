namespace ChessEngine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Threading.Tasks;
    using TestFrameworkCore.Assertions;
    using ChessEngine;
    using TestFrameworkCore.Attributes;
    using TestFrameworkCore.Contexts;

    namespace ChessEngineTests
    {
        // Вспомогательный класс для создания тестовых позиций
        public static class TestPositionBuilder
        {
            public static Pieces CreateEmptyBoard()
            {
                var position = new Position("8/8/8/8/8/8/8/8");
                return position.Pieces;
            }

            public static Pieces CreatePositionWithPiece(byte square, PieceColor color, PieceType type)
            {
                var position = new Position("8/8/8/8/8/8/8/8");
                var pieces = position.Pieces;
                pieces.PieceBitboards[(int)color, (int)type].Value |= 1UL << square;
                pieces.SideBitboards[(int)color].Value |= 1UL << square;
                pieces.All.Value |= 1UL << square;
                pieces.UpdateBitboards();
                return pieces;
            }

            public static Pieces CreatePositionWithPieces(Dictionary<byte, (PieceColor color, PieceType type)> pieces)
            {
                var position = new Position("8/8/8/8/8/8/8/8");
                var result = position.Pieces;
                foreach (var kvp in pieces)
                {
                    byte square = kvp.Key;
                    var (color, type) = kvp.Value;

                    result.PieceBitboards[(int)color, (int)type].Value |= 1UL << square;
                    result.SideBitboards[(int)color].Value |= 1UL << square;
                    result.All.Value |= 1UL << square;
                }
                result.UpdateBitboards();
                return result;
            }
        }

        [TestClass(Category = "BishopMoves", Priority = 1)]
        public class BishopMoveTests
        {
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

                // Проверяем несколько ключевых квадратов
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
                ulong moves = PsLegalMoves.GenerateBishopMask(pieces, e4, PieceColor.White, false);

                // Assert
                // Должен включать f5 (так как на f5 своя пешка, но нельзя пройти дальше)
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
            public void RookMoves_FromCenterEmptyBoard_Returns14Moves()
            {
                // Arrange
                byte e4 = 36;
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong moves = PsLegalMoves.GenerateRookMask(emptyBoard, e4, PieceColor.White, false);

                // Assert
                int moveCount = BitOperations.PopCount(moves);
                Assert.AreEqual(14, moveCount, "Rook from center should have 14 moves on empty board");

                // Проверяем вертикаль и горизонталь
                for (int i = 1; i <= 4; i++)
                {
                    Assert.IsTrue((moves & (1UL << (36 + i * 8))) != 0, $"Should include square {36 + i * 8} (north)");
                    Assert.IsTrue((moves & (1UL << (36 - i * 8))) != 0, $"Should include square {36 - i * 8} (south)");
                }

                for (int i = 1; i <= 4; i++)
                {
                    if (36 + i <= 39) Assert.IsTrue((moves & (1UL << (36 + i))) != 0, $"Should include square {36 + i} (east)");
                    if (36 - i >= 32) Assert.IsTrue((moves & (1UL << (36 - i))) != 0, $"Should include square {36 - i} (west)");
                }
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

                // На юг должен включать e3 (своя), но не e2
                Assert.IsTrue((moves & (1UL << 28)) == 0, "Should not include e3 (own piece as blocker)");
                Assert.IsFalse((moves & (1UL << 20)) != 0, "Should not include e2 (beyond own piece)");
            }
        }

        [TestClass(Category = "QueenMoves", Priority = 1)]
        public class QueenMoveTests
        {
            [TestMethod]
            public void QueenMoves_FromCenterEmptyBoard_Returns27Moves()
            {
                // Arrange
                byte e4 = 36;
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong moves = PsLegalMoves.GenerateQueenMask(emptyBoard, e4, PieceColor.White, false);

                // Assert
                int moveCount = BitOperations.PopCount(moves);
                Assert.AreEqual(27, moveCount, "Queen from center should have 27 moves on empty board");
            }

            [TestMethod]
            public void QueenMoves_IsCombinationOfBishopAndRook()
            {
                // Arrange
                byte e4 = 36;
                var emptyBoard = TestPositionBuilder.CreateEmptyBoard();

                // Act
                ulong queenMoves = PsLegalMoves.GenerateQueenMask(emptyBoard, e4, PieceColor.White, false);
                ulong bishopMoves = PsLegalMoves.GenerateBishopMask(emptyBoard, e4, PieceColor.White, false);
                ulong rookMoves = PsLegalMoves.GenerateRookMask(emptyBoard, e4, PieceColor.White, false);

                // Assert
                Assert.AreEqual(bishopMoves | rookMoves, queenMoves,
                    "Queen moves should be union of bishop and rook moves");
            }
        }

        [TestClass(Category = "Integration", Priority = 3)]
        public class IntegrationTests
        {
            private static SharedContext _sharedContext;

            [BeforeAll]
            public static void InitializeSharedContext()
            {
                _sharedContext = SharedContext.Create();
                _sharedContext.SetData("TestSuite", "ChessEngineTests");
                _sharedContext.SetData("TotalTestsRun", 0);
                Console.WriteLine("Shared context initialized for ChessEngineTests");
            }

            [BeforeEach]
            public void Setup()
            {
                if (_sharedContext != null)
                {
                    int count = _sharedContext.GetData<int>("TotalTestsRun") + 1;
                    _sharedContext.SetData("TotalTestsRun", count);
                }
            }

            [TestMethod]
            public void SharedContext_StoresTestCount()
            {
                int count = _sharedContext.GetData<int>("TotalTestsRun");
                Assert.IsTrue(count > 0, "Test count should be > 0");
            }

            [TestMethod]
            public void SharedContext_RetrievesTestSuite()
            {
                string? suite = _sharedContext.GetData<string>("TestSuite");
                Assert.AreEqual("ChessEngineTests", suite, "Should retrieve correct test suite name");
            }

            [TestMethod]
            public void ComplexPosition_AllMovesGenerated()
            {
                // Создаем сложную позицию с несколькими фигурами
                var pieces = TestPositionBuilder.CreatePositionWithPieces(new Dictionary<byte, (PieceColor, PieceType)>
                {
                    // Белые фигуры
                    { 7, (PieceColor.White, PieceType.Rook) },     // a1
                    { 3, (PieceColor.White, PieceType.King) },     // e1
                    { 14, (PieceColor.White, PieceType.Pawn) },     // b2
                    { 28, (PieceColor.White, PieceType.Bishop) },  // d4
                
                    // Черные фигуры
                    { 37, (PieceColor.Black, PieceType.Knight) },  // c5
                    { 51, (PieceColor.Black, PieceType.Queen) },   // e7
                    { 56, (PieceColor.Black, PieceType.Rook) },    // h8
                });

                // Проверяем несколько ходов
                ulong whiteRookMoves = PsLegalMoves.GenerateRookMask(pieces, 0, PieceColor.White, false);
                ulong blackQueenMoves = PsLegalMoves.GenerateQueenMask(pieces, 52, PieceColor.Black, false);
                bool isE1Attacked = PsLegalMoves.IsSquareUnderAttack(pieces, 4, PieceColor.White);

                Assert.IsTrue(whiteRookMoves != 0, "White rook should have moves");
                Assert.IsTrue(blackQueenMoves != 0, "Black queen should have moves");
                // В этой позиции e1 не должен быть под атакой
                Assert.IsFalse(isE1Attacked, "e1 should not be under attack");
            }

            [AfterAll]
            public static void CleanupSharedContext()
            {
                if (_sharedContext != null)
                {
                    int totalTests = _sharedContext.GetData<int>("TotalTestsRun");
                    Console.WriteLine($"Total tests executed in suite: {totalTests}");
                    Console.WriteLine($"Shared context was used {_sharedContext.InitializationCount} times");
                    _sharedContext.Dispose();
                }
            }
        }

        [TestClass(Category = "AdvancedScenarios", Priority = 2)]
        public class AdvancedScenarioTests
        {
            [TestMethod]
            public void Test_QueenMoves_WithMultipleBlockers_ComplexAssertions()
            {
                // Arrange - сложная позиция с множеством блокировок
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
                ulong queenMoves = PsLegalMoves.GenerateQueenMask(pieces, d4, PieceColor.White, false);
                ulong queenCaptures = PsLegalMoves.GenerateQueenMask(pieces, d4, PieceColor.White, true);

                // Собираем статистику
                var moveList = new List<byte>();
                var captureList = new List<byte>();

                for (byte i = 0; i < 64; i++)
                {
                    if ((queenMoves & (1UL << i)) != 0)
                        moveList.Add(i);
                    if ((queenCaptures & (1UL << i)) != 0)
                        captureList.Add(i);
                }

                // Assert - демонстрируем все Assert-ы

                // IsTrue / IsFalse
                Assert.IsTrue(queenMoves != 0, "Queen should have moves");
                Assert.IsFalse(queenMoves == 0, "Queen moves should not be zero");

                // AreEqual / AreNotEqual
                Assert.AreNotEqual(queenMoves, queenCaptures, "All moves and captures should differ");
                Assert.AreEqual(2, captureList.Count, "Should have exactly 2 captures");

                // IsNull / IsNotNull
                Assert.IsNotNull(moveList, "Move list should not be null");
                Assert.IsNotNull(captureList, "Capture list should not be null");

                // Contains
                Assert.Contains<byte>(moveList, 43, "Should contain d6 (enemy - capture)");
                Assert.Contains<byte>(moveList, 45, "Should contain f5 (enemy - capture)");

                // Не должны содержать квадраты за блокирующими фигурами
                Assert.IsFalse(moveList.Contains(11), "Should not contain d2 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(21), "Should not contain f4 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(25), "Should not contain b4 (behind own pawn)");
                Assert.IsFalse(moveList.Contains(51), "Should not contain d7 (behind enemy)");

                // InRange
                Assert.InRange(moveList.Count, 5, 15, "Move count should be in reasonable range");
                Assert.InRange(captureList.Count, 1, 3, "Capture count should be in reasonable range");

                // Throws / DoesNotThrow
                Assert.Throws<InvalidOperationException>(() => PsLegalMoves.ValidatePosition(pieces, 24, PieceColor.White),
                    "Should throw for invalid square");
                Assert.DoesNotThrow(
                    () => PsLegalMoves.ValidatePosition(pieces, 27, PieceColor.White),
                    "Should throw for invalid square");
            }
        }
    }
}

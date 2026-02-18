using ChessEngine.Tests.ChessEngineTests;
using System.Numerics;
using TestFrameworkCore.Assertions;
using TestFrameworkCore.Attributes;
using TestFrameworkCore.Contexts;

namespace ChessEngine.Tests
{
    [TestClass(Category = "Integration", Priority = 3)]
    public class IntegrationTests
    {
        private static SharedContext _sharedContext;

        [BeforeAll]
        public static void InitializeSharedContext()
        {
            _sharedContext = SharedContext.Create();

            // Инициализируем разделяемые данные для всей тестовой сессии
            var initialPosition = new Dictionary<byte, (PieceColor, PieceType)>
                {
                    { 0, (PieceColor.White, PieceType.Rook) },
                    { 1, (PieceColor.White, PieceType.Knight) },
                    { 2, (PieceColor.White, PieceType.Bishop) },
                    { 3, (PieceColor.White, PieceType.Queen) },
                    { 4, (PieceColor.White, PieceType.King) },
                    { 5, (PieceColor.White, PieceType.Bishop) },
                    { 6, (PieceColor.White, PieceType.Knight) },
                    { 7, (PieceColor.White, PieceType.Rook) },
                    { 8, (PieceColor.White, PieceType.Pawn) },
                    { 9, (PieceColor.White, PieceType.Pawn) },
                    { 10, (PieceColor.White, PieceType.Pawn) },
                    { 11, (PieceColor.White, PieceType.Pawn) },
                    { 12, (PieceColor.White, PieceType.Pawn) },
                    { 13, (PieceColor.White, PieceType.Pawn) },
                    { 14, (PieceColor.White, PieceType.Pawn) },
                    { 15, (PieceColor.White, PieceType.Pawn) },

                    { 48, (PieceColor.Black, PieceType.Pawn) },
                    { 49, (PieceColor.Black, PieceType.Pawn) },
                    { 50, (PieceColor.Black, PieceType.Pawn) },
                    { 51, (PieceColor.Black, PieceType.Pawn) },
                    { 52, (PieceColor.Black, PieceType.Pawn) },
                    { 53, (PieceColor.Black, PieceType.Pawn) },
                    { 54, (PieceColor.Black, PieceType.Pawn) },
                    { 55, (PieceColor.Black, PieceType.Pawn) },
                    { 56, (PieceColor.Black, PieceType.Rook) },
                    { 57, (PieceColor.Black, PieceType.Knight) },
                    { 58, (PieceColor.Black, PieceType.Bishop) },
                    { 59, (PieceColor.Black, PieceType.Queen) },
                    { 60, (PieceColor.Black, PieceType.King) },
                    { 61, (PieceColor.Black, PieceType.Bishop) },
                    { 62, (PieceColor.Black, PieceType.Knight) },
                    { 63, (PieceColor.Black, PieceType.Rook) }
                };

            var pieces = TestPositionBuilder.CreatePositionWithPieces(initialPosition);

            // Сохраняем начальную позицию в shared context
            _sharedContext.SetData("StartingPosition", pieces);
            _sharedContext.SetData("MoveHistory", new List<string>());
            _sharedContext.SetData("CapturedPieces", new Dictionary<PieceColor, List<PieceType>>());
            _sharedContext.SetData("PositionHash", pieces.All.Value.GetHashCode());
        }

        [BeforeEach]
        public void Setup()
        {
            var history = _sharedContext.GetData<List<string>>("MoveHistory");
        }

        [TestMethod]
        public void SharedContext_PositionConsistency_AcrossTests()
        {
            // Получаем позицию из shared context
            var position = _sharedContext.GetData<Pieces>("StartingPosition");
            var currentHash = _sharedContext.GetData<int>("PositionHash");

            // Проверяем, что позиция не изменилась
            Assert.IsNotNull(position, "Starting position should exist in shared context");
            Assert.AreEqual(currentHash, position.All.Value.GetHashCode(),
                "Position hash should remain consistent across tests");

            // Проверяем количество фигур в начальной позиции
            int totalPieces = BitOperations.PopCount(position.All.Value);
            Assert.AreEqual(32, totalPieces, "Starting position should have 32 pieces");

            // Проверяем, что у белых 16 фигур
            int whitePieces = BitOperations.PopCount(position.SideBitboards[(int)PieceColor.White].Value);
            Assert.AreEqual(16, whitePieces, "White should have 16 pieces in starting position");

            // Добавляем информацию в историю
            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            history?.Add($"Test 'PositionConsistency' verified starting position with {totalPieces} pieces");
        }

        [TestMethod]
        public void SharedContext_MoveHistory_AccumulatesData()
        {
            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            var captured = _sharedContext.GetData<Dictionary<PieceColor, List<PieceType>>>("CapturedPieces");

            // Имитируем несколько ходов и обновляем shared context
            var moves = new[]
            {
                "e2-e4",
                "e7-e5",
                "g1-f3",
                "b8-c6"
            };

            foreach (var move in moves)
            {
                history.Add($"Move: {move}");
            }

            // Добавляем информацию о взятии
            if (!captured.ContainsKey(PieceColor.Black))
                captured[PieceColor.Black] = new List<PieceType>();
            captured[PieceColor.Black].Add(PieceType.Pawn); // Имитируем взятие пешки

            // Проверяем, что история накапливается
            Assert.IsTrue(history.Contains("Move: e2-e4"), "Should contain recorded moves");

            // Проверяем, что данные о взятиях сохраняются
            Assert.IsTrue(captured.ContainsKey(PieceColor.Black), "Should have captured pieces data");
            Assert.Contains(captured[PieceColor.Black], PieceType.Pawn, "Should record captured pawn");
        }

        [TestMethod]
        public void SharedContext_PositionEvaluation_CachedInContext()
        {
            var position = _sharedContext.GetData<Pieces>("StartingPosition");

            double evaluation = PsLegalMoves.EvaluatePosition(position);
            _sharedContext.SetData("PositionEvaluation", evaluation);

            Assert.InRange((int)evaluation, -10, 10, "Position evaluation should be in reasonable range");

            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            history?.Add($"Position evaluation: {evaluation:F2}");
        }

        [TestMethod]
        public void SharedContext_AttackMaps_SharedBetweenTests()
        {
            var position = _sharedContext.GetData<Pieces>("StartingPosition");

            // Вычисляем карту атак для белого короля
            byte whiteKingSquare = PsLegalMoves.FindKingSquare(position, PieceColor.White);
            bool isKingAttacked = PsLegalMoves.IsSquareUnderAttack(position, whiteKingSquare, PieceColor.White);

            // Сохраняем результат в shared context
            var attackMaps = _sharedContext.GetData<Dictionary<string, bool>>("AttackMaps");
            if (attackMaps == null)
            {
                attackMaps = new Dictionary<string, bool>();
                _sharedContext.SetData("AttackMaps", attackMaps);
            }

            attackMaps["WhiteKingAttacked"] = isKingAttacked;

            // В начальной позиции король не должен быть под атакой
            Assert.IsFalse(isKingAttacked, "White king should not be under attack in starting position");

            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            history?.Add($"Attack map calculated for white king at square {whiteKingSquare}");
        }

        [TestMethod]
        public void SharedContext_ComplexAnalysis_AggregatesResults()
        {
            var position = _sharedContext.GetData<Pieces>("StartingPosition");
            var attackMaps = _sharedContext.GetData<Dictionary<string, bool>>("AttackMaps")
                            ?? new Dictionary<string, bool>();

            // Анализируем все фигуры
            var analysis = new Dictionary<string, object>();

            // Считаем мобильность фигур
            int whiteMobility = PsLegalMoves.CalculateMobility(position, PieceColor.White);
            int blackMobility = PsLegalMoves.CalculateMobility(position, PieceColor.Black);

            analysis["WhiteMobility"] = whiteMobility;
            analysis["BlackMobility"] = blackMobility;
            analysis["MobilityDifference"] = whiteMobility - blackMobility;

            // Сохраняем анализ
            _sharedContext.SetData("PositionAnalysis", analysis);

            // Проверяем, что анализ разумный
            Assert.IsTrue(whiteMobility > 0, "White should have some moves");
            Assert.IsTrue(blackMobility > 0, "Black should have some moves");

            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            history?.Add($"Mobility analysis: White={whiteMobility}, Black={blackMobility}");
        }

        [TestMethod]
        public void SharedContext_AllData_AccessibleFromAnyTest()
        {
            // Проверяем, что все данные, накопленные в предыдущих тестах, доступны

            var position = _sharedContext.GetData<Pieces>("StartingPosition");
            var history = _sharedContext.GetData<List<string>>("MoveHistory");
            var captured = _sharedContext.GetData<Dictionary<PieceColor, List<PieceType>>>("CapturedPieces");

            // Проверяем наличие всех ключей
            Assert.IsNotNull(position, "Starting position should be accessible");
            Assert.IsNotNull(history, "Move history should be accessible");
            Assert.IsNotNull(captured, "Captured pieces data should be accessible");

            // Проверяем, что данные накопились
            Assert.IsTrue(history.Count >= 5, $"History should have accumulated entries (has {history.Count})");

            // Добавляем финальную запись в историю
            history.Add($"=== Test Suite Completed: {DateTime.Now} ===");
        }

        [AfterAll]
        public static void CleanupSharedContext()
        {
            if (_sharedContext != null)
            {

                var history = _sharedContext.GetData<List<string>>("MoveHistory");
                var captured = _sharedContext.GetData<Dictionary<PieceColor, List<PieceType>>>("CapturedPieces");
                var analysis = _sharedContext.GetData<Dictionary<string, object>>("PositionAnalysis");

                // Сохраняем историю в файл
                if (history != null && history.Count > 0)
                {
                    string historyPath = $"chess_test_history_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    File.WriteAllLines(historyPath, history);
                    Console.WriteLine($"Test history saved to: {historyPath}");
                }

                _sharedContext.Dispose();
            }
        }
    }
}
using ChessEngine.Tests.Contexts;
using System.Numerics;
using TestFrameworkCore.Assertions;
using TestFrameworkCore.Attributes;

namespace ChessEngine.Tests
{
    [NoParallel]
    [SharedContext(typeof(ChessEngineTestContext))]
    [TestClass(Category = "Integration")]
    public class ChessEngineIntegrationTests
    {
        private static ChessEngineTestContext _context;

        [BeforeAll]
        public static void Initialize(ChessEngineTestContext context)
        {
            _context = context;
        }

        [BeforeEach]
        public void Setup()
        {
            _context.MoveHistory.Add($"--- Starting test at {DateTime.Now:HH:mm:ss} ---");
        }

        [TestMethod(Priority = 1)]
        public void StartingPosition_ShouldBeValid()
        {
            Pieces? position = _context.StartingPosition;

            Assert.IsNotNull(position);
            Assert.AreEqual(_context.InitialHash, position!.All.Value.GetHashCode());

            int totalPieces = BitOperations.PopCount(position.All.Value);
            Assert.AreEqual(32, totalPieces);

            int whitePieces = BitOperations.PopCount(position.SideBitboards[(int)PieceColor.White].Value);

            Assert.AreEqual(16, whitePieces);

            _context.MoveHistory.Add("Starting position validated.");
        }

        [TestMethod(Priority = 2)]
        public void Mobility_ShouldBeCalculated_AndCached()
        {
            var position = _context.StartingPosition;

            int whiteMobility = PsLegalMoves.CalculateMobility(position!, PieceColor.White);
            int blackMobility = PsLegalMoves.CalculateMobility(position!, PieceColor.Black);

            Assert.AreEqual(whiteMobility, 132);
            Assert.AreEqual(blackMobility, 132);

            _context.AnalysisCache["WhiteMobility"] = whiteMobility;
            _context.AnalysisCache["BlackMobility"] = blackMobility;

            _context.MoveHistory.Add($"Mobility W:{whiteMobility} B:{blackMobility}");
        }

        [TestMethod(Priority = 2)]
        public void King_ShouldNotBeUnderAttack_InStartingPosition()
        {
            var position = _context.StartingPosition;

            byte whiteKingSquare = PsLegalMoves.FindKingSquare(position!, PieceColor.White);

            bool attacked = PsLegalMoves.IsSquareUnderAttack(position!, whiteKingSquare, PieceColor.White);

            Assert.IsFalse(attacked);

            _context.AnalysisCache["WhiteKingSafe"] = true;
            _context.MoveHistory.Add("White king safety verified.");
        }

        [TestMethod(Priority = 3)]
        public void Context_ShouldAccumulate_DataAcrossTests()
        {
            Assert.IsTrue(_context.MoveHistory.Count > 0);

            Assert.IsTrue(_context.AnalysisCache.ContainsKey("WhiteMobility"));
            Assert.IsTrue(_context.AnalysisCache.ContainsKey("WhiteKingSafe"));

            _context.MoveHistory.Add("Accumulated data verified.");
        }

        [AfterAll]
        public static void Cleanup()
        {
            if (_context.MoveHistory.Count > 0)
            {
                string historyPath = $"chess_integration_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                File.WriteAllLines(historyPath, _context.MoveHistory);
                Console.WriteLine($"History saved: {historyPath}");
            }
        }
    }
}
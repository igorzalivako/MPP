using TestFrameworkCore.Interfaces;
using static ChessEngine.PsLegalMoves;

namespace ChessEngine.Tests.Contexts
{
    public class ChessEngineTestContext : IDisposable, ISharedContext
    {
        public Pieces? StartingPosition { get; private set; }
        
        public List<string> MoveHistory { get; } = new();
        
        public Dictionary<PieceColor, List<PieceType>> CapturedPieces { get; } = [];

        public Dictionary<string, object> AnalysisCache { get; } = [];

        public int InitialHash { get; private set; }

        public void Initialize()
        {
            var initialPosition = TestPositionBuilder.CreateStandardPosition();

            StartingPosition = initialPosition;
            InitialHash = initialPosition.All.Value.GetHashCode();
        }

        public void Dispose()
        {
            MoveHistory.Clear();
            CapturedPieces.Clear();
            AnalysisCache.Clear();
        }
    }
}

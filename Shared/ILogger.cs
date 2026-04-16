using TestFrameworkCore.Results;

namespace Shared
{
    public interface ILogger
    {
        void Log(string message);

        void PrintSingleResult(TestResult r);

        void PrintSummary(IReadOnlyCollection<TestResult> results, TimeSpan totalDuration);

        void PrintResults(IReadOnlyCollection<TestResult> results, TimeSpan totalDuration);

    }
}

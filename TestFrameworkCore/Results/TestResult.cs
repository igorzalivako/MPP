namespace TestFrameworkCore.Results
{
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public TimeSpan Duration { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Category { get; set; } = string.Empty;

        public int Priority { get; set; }
    }
}

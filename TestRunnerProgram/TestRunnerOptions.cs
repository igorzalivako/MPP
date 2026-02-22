namespace TestRunnerProgram
{
    public class TestRunnerOptions
    {
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }
}

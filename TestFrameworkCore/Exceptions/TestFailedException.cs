namespace TestFrameworkCore.Exceptions
{
    public class TestFailedException : Exception
    {
        public TestFailedException(string message, Exception inner) : base(message, inner) { }

        public TestFailedException(string message) : base(message) { }
    }
}

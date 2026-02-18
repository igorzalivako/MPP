namespace TestFrameworkCore.Exceptions
{
    public class AssertionFailedException : Exception
    {
        public AssertionFailedException(string message, Exception inner) : base(message, inner) { }

        public AssertionFailedException(string message) : base(message) { }
    }
}

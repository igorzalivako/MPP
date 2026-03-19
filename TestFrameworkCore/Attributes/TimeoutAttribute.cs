namespace TestFrameworkCore.Attributes
{
    namespace TestFrameworkCore.Attributes
    {
        [AttributeUsage(AttributeTargets.Method)]
        public class TimeoutAttribute : Attribute
        {
            public int Milliseconds { get; }

            public TimeoutAttribute(int milliseconds)
            {
                Milliseconds = milliseconds;
            }
        }
    }
}

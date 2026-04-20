namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestCaseSourceAttribute : Attribute
    {
        public string SourceName { get; }

        public TestCaseSourceAttribute(string sourceName)
        {
            SourceName = sourceName;
        }
    }
}
namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
        public string? Category { get; set; }
    }
}
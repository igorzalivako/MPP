namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string? Description { get; set; }

        public int Priority { get; set; } = 0;
    }
}
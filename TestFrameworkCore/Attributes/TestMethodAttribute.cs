namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string? Description { get; set; }

        public bool IsCritical { get; set; }
    }
}
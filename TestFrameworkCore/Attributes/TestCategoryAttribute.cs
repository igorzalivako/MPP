namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestCategoryAttribute : Attribute
    {
        public string Name { get; set; } = "";
    }
}

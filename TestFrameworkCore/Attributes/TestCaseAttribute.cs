namespace TestFrameworkCore.Attributes
{
    // Маркер для параметризованных тестов
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Parameters { get; }
        public string? Name { get; set; }

        public TestCaseAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }
    }

}
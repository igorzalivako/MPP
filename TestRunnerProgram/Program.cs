using System.Reflection;
using TestRunnerProgram.TestRunners;

namespace TestRunnerProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            string? pathToAssembly;
            do
            {
                Console.WriteLine("Введите путь к тестовой сборке:\n");
                do
                {
                    pathToAssembly = Console.ReadLine();
                } while (pathToAssembly == null);

                try
                {
                    RunTestsFromAssembly(pathToAssembly);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            } while (pathToAssembly != string.Empty);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void RunTestsFromAssembly(string pathToAssembly)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Loading tests...\n");
            Console.ResetColor();

            var testRunner = new TestRunner();

            var currentAssembly = Assembly.LoadFrom(pathToAssembly);

            testRunner.RunTestsInAssembly(currentAssembly);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string resultsPath = $"chess_test_results_{timestamp}.txt";
            testRunner.PrintResults();
        }
    }
}
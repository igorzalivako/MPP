using System.Reflection;
using TestRunnerProgram.TestRunners;

namespace TestRunnerProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Chess Engine Test Framework Runner ===\n");

            try
            {
                // В реальном проекте здесь должен быть путь к сборке с тестами
                // Для демонстрации будем использовать текущую сборку
                RunTestsFromCurrentAssembly();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void RunTestsFromCurrentAssembly()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Loading tests from ChessEngine...\n");
            Console.ResetColor();

            var testRunner = new TestRunner();
            var currentAssembly = Assembly.LoadFrom("D:\\3 курс 2 семестр\\СПП\\MPP\\ChessEngine.Tests\\bin\\Debug\\net9.0\\ChessEngine.Tests.dll");

            // Запускаем тесты
            var results = testRunner.RunTestsInAssembly(currentAssembly);

            // Сохраняем результаты в файл с отметкой времени
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string resultsPath = $"chess_test_results_{timestamp}.txt";
            testRunner.PrintResults(resultsPath);

            // Дополнительная статистика
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n=== Test Categories Summary ===");
            Console.ResetColor();

            var categories = new Dictionary<string, int>();
            foreach (var result in results)
            {
                string category = "Unknown";
                if (result.TestName.Contains("Knight")) category = "Knight";
                else if (result.TestName.Contains("Bishop")) category = "Bishop";
                else if (result.TestName.Contains("Rook")) category = "Rook";
                else if (result.TestName.Contains("Queen")) category = "Queen";
                else if (result.TestName.Contains("King")) category = "King";
                else if (result.TestName.Contains("Pawn")) category = "Pawn";
                else if (result.TestName.Contains("Attack")) category = "Attack";
                else if (result.TestName.Contains("Edge")) category = "Edge Cases";
                else if (result.TestName.Contains("Integration")) category = "Integration";
                else category = "Other";

                if (!categories.ContainsKey(category))
                    categories[category] = 0;
                categories[category]++;
            }

            foreach (var cat in categories)
            {
                Console.Write($"{cat.Key}: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{cat.Value} tests");
                Console.ResetColor();
            }
        }
    }
}
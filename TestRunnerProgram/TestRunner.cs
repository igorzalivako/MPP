using System.Reflection;
using TestFrameworkCore.Attributes;
using TestFrameworkCore.Exceptions;
using TestFrameworkCore.Results;

namespace TestRunnerProgram.TestRunners
{
    public class TestRunner
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        public IEnumerable<TestResult> RunTestsInAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return RunTestsInAssembly(assembly);
        }

        public IEnumerable<TestResult> RunTestsInAssembly(Assembly assembly)
        {
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestClassAttribute>().Any());

            foreach (var testClass in testClasses)
            {
                var results = RunTestsInClass(testClass);
                _results.AddRange(results);
            }

            return _results;
        }

        private static List<TestResult> RunTestsInClass(Type testClass)
        {
            var testMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes<TestMethodAttribute>().Any());

            var testObject = Activator.CreateInstance(testClass);
            var results = new List<TestResult>();

            if (testObject != null)
            {
                // Выполняем BeforeAll методы
                ExecuteLifecycleMethods(testClass, testObject, typeof(BeforeAllAttribute));

                foreach (var method in testMethods)
                {
                    var testResult = ExecuteTestMethod(testClass, testObject, method);
                    results.Add(testResult);
                }

                // Выполняем AfterAll методы
                ExecuteLifecycleMethods(testClass, testObject, typeof(AfterAllAttribute));
            }

            return results;
        }

        private static TestResult ExecuteTestMethod(Type testClass, object testObject, MethodInfo method)
        {
            var testResult = new TestResult
            {
                TestName = $"{testClass.Name}.{method.Name}",
                StartTime = DateTime.Now
            };

            try
            {
                // Выполняем BeforeEach методы
                ExecuteLifecycleMethods(testClass, testObject, typeof(BeforeEachAttribute));

                // Проверяем наличие параметризованных тестов
                var testCases = method.GetCustomAttributes<TestCaseAttribute>().ToList();

                if (testCases.Count != 0)
                {
                    foreach (var testCase in testCases)
                    {
                        testResult.TestName = $"{testClass.Name}.{method.Name}[{testCase.Name ?? string.Join(",", testCase.Parameters)}]";

                        // Выполняем BeforeEach перед каждым тест-кейсом
                        ExecuteLifecycleMethods(testClass, testObject, typeof(BeforeEachAttribute));

                        method.Invoke(testObject, testCase.Parameters);

                        // Выполняем AfterEach после каждого тест-кейса
                        ExecuteLifecycleMethods(testClass, testObject, typeof(AfterEachAttribute));
                    }
                }
                else
                {
                    // Обычный тест
                    if (method.ReturnType == typeof(Task))
                    {
                        // Асинхронный тест
                        var task = (Task?)method.Invoke(testObject, null);
                        task?.Wait();
                    }
                    else
                    {
                        // Синхронный тест
                        method.Invoke(testObject, null);
                    }
                }

                testResult.Passed = true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is AssertionFailedException)
            {
                testResult.Passed = false;
                testResult.ErrorMessage = $"Assertion failed: {ex.InnerException.Message}";
            }
            catch (Exception ex)
            {
                testResult.Passed = false;
                testResult.ErrorMessage = $"Test failed with exception: {ex.InnerException?.Message ?? ex.Message}";
            }
            finally
            {
                // Выполняем AfterEach методы
                ExecuteLifecycleMethods(testClass, testObject, typeof(AfterEachAttribute));

                testResult.EndTime = DateTime.Now;
                testResult.Duration = testResult.EndTime - testResult.StartTime;
            }

            return testResult;
        }

        private static void ExecuteLifecycleMethods(Type testClass, object testObject, Type attributeType)
        {
            var lifecycleMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes(attributeType).Any());

            foreach (var method in lifecycleMethods)
            {
                method.Invoke(testObject, null);
            }
        }

        public void PrintResults(string outputPath = null)
        {
            var output = new System.Text.StringBuilder();

            // Заголовок
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== TEST RESULTS ===");
            Console.ResetColor();

            // Статистика
            int passed = _results.Count(r => r.Passed);
            int failed = _results.Count(r => !r.Passed);

            Console.WriteLine($"Total tests: {_results.Count}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed: {passed}");
            Console.ResetColor();

            if (failed > 0)
                Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed: {failed}");
            Console.ResetColor();

            Console.WriteLine();

            // Результаты каждого теста
            foreach (var result in _results)
            {
                // Статус теста (PASS/FAIL) с цветом
                if (result.Passed)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.Write($"[{(result.Passed ? "PASS" : "FAIL")}] ");
                Console.ResetColor();

                // Имя теста
                Console.WriteLine(result.TestName);

                // Длительность (желтым цветом)
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Duration: {result.Duration.TotalMilliseconds:F2}ms");
                Console.ResetColor();

                // Ошибка для упавших тестов (красным)
                if (!result.Passed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  Error: {result.ErrorMessage}");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Сохраняем в файл без цветов
                output.AppendLine($"[{(result.Passed ? "PASS" : "FAIL")}] {result.TestName}");
                output.AppendLine($"  Duration: {result.Duration.TotalMilliseconds:F2}ms");
                if (!result.Passed)
                {
                    output.AppendLine($"  Error: {result.ErrorMessage}");
                }
                output.AppendLine();
            }

            // Сохранение в файл
            if (outputPath != null)
            {
                File.WriteAllText(outputPath, output.ToString());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nResults written to: {outputPath}");
                Console.ResetColor();
            }
        }
    }
}

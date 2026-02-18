using System.Reflection;
using TestFrameworkCore.Attributes;
using TestFrameworkCore.Exceptions;
using TestFrameworkCore.Results;

namespace TestRunnerProgram.TestRunners
{
    public class TestRunner
    {
        private readonly List<TestResult> _results = [];

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
                    var methodAttr = method.GetCustomAttributes<SkipTestAttribute>();
                    if (!methodAttr.Any())
                    {
                        var testResult = ExecuteTestMethod(testClass, testObject, method);
                        results.Add(testResult);
                    }
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
                StartTime = DateTime.Now,
                Passed = true
            };

            var classAttr = testClass.GetCustomAttribute<TestClassAttribute>();
            if (classAttr != null)
            {
                testResult.Category = classAttr.Category ?? "Uncategorized";
                testResult.Priority = classAttr.Priority;
            }

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
                        var currentTestName = $"{testClass.Name}.{method.Name}[{testCase.Name ?? string.Join(",", testCase.Parameters)}]";

                        try
                        {
                            // Выполняем BeforeEach перед каждым тест-кейсом
                            ExecuteLifecycleMethods(testClass, testObject, typeof(BeforeEachAttribute));
                            method.Invoke(testObject, testCase.Parameters);
                            testResult.TestName += $"\n\t{currentTestName}\n";
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is AssertionFailedException)
                        {
                            testResult.Passed = false;
                            testResult.ErrorMessage += $"\n\t{currentTestName} Assertion failed: {ex.InnerException.Message}\n";
                        }
                        finally
                        {
                            // Выполняем AfterEach после каждого тест-кейса
                            ExecuteLifecycleMethods(testClass, testObject, typeof(AfterEachAttribute));
                        }
                    }
                }
                else
                {
                    testResult.TestName = $"{testClass.Name}.{method.Name}";
                        
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

        public void PrintResults()
        {
            if (_results == null) throw new ArgumentNullException(nameof(_results));

            if (_results.Count == 0)
            {
                WriteLineColored("No test results.", ConsoleColor.DarkYellow);
                return;
            }

            bool canUseColors = !Console.IsOutputRedirected;

            // Общая сводка
            int total = _results.Count;
            int passed = _results.Count(r => r.Passed);
            int failed = total - passed;
            TimeSpan totalDuration = new TimeSpan(_results.Sum(r => r.Duration.Ticks));

            WriteHeader("TEST RUN SUMMARY", canUseColors);
            WriteKeyValue("Total", total.ToString(), ConsoleColor.Gray, canUseColors);
            WriteKeyValue("Passed", passed.ToString(), ConsoleColor.Green, canUseColors);
            WriteKeyValue("Failed", failed.ToString(), ConsoleColor.Red, canUseColors);
            WriteKeyValue("Duration", FormatDuration(totalDuration), ConsoleColor.Cyan, canUseColors);
            Console.WriteLine();

            // Группировка по категориям (пустые категории -> "Uncategorized")
            var groups = _results
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Category) ? "Uncategorized" : r.Category.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var group in groups)
            {
                var items = group
                    .OrderBy(r => r.Passed) // сначала failed, потом passed (по желанию можно наоборот)
                    .ThenByDescending(r => r.Priority)
                    .ThenBy(r => r.TestName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                int gTotal = items.Length;
                int gPassed = items.Count(r => r.Passed);
                int gFailed = gTotal - gPassed;
                TimeSpan gDuration = new TimeSpan(items.Sum(r => r.Duration.Ticks));

                // Заголовок категории
                WriteCategoryHeader(group.Key, gTotal, gPassed, gFailed, gDuration, canUseColors);

                // Небольшая табличка
                // Колонки: Status | Name | Priority | Duration | Start - End
                foreach (var r in items)
                {
                    var statusText = r.Passed ? "PASS" : "FAIL";
                    var statusColor = r.Passed ? ConsoleColor.Green : ConsoleColor.Red;

                    // Строка результата
                    WriteColored($"  [{statusText}]", statusColor, canUseColors);
                    Console.Write(" ");

                    // Имя
                    Console.Write(r.TestName);

                    // Доп. поля в конце (чтобы имя не резать)
                    Console.Write($"  (P{r.Priority})");
                    Console.Write($"  {FormatDuration(r.Duration)}");
                    Console.Write($"  {r.StartTime:HH:mm:ss}–{r.EndTime:HH:mm:ss}");
                    Console.WriteLine();

                    // Ошибка — отдельной строкой, если есть
                    if (!r.Passed && !string.IsNullOrWhiteSpace(r.ErrorMessage))
                    {
                        WriteColored("      ", ConsoleColor.DarkRed, canUseColors);
                        WriteLineColored(r.ErrorMessage.Trim(), ConsoleColor.DarkRed, canUseColors);
                    }
                }

                Console.WriteLine();
            }
        }

        private static void WriteHeader(string title, bool canUseColors)
        {
            WriteLineColored(new string('=', Math.Max(10, title.Length + 8)), ConsoleColor.DarkGray);
            WriteColored("=== ", ConsoleColor.DarkGray, canUseColors);
            WriteColored(title, ConsoleColor.White, canUseColors);
            WriteLineColored(" ===", ConsoleColor.DarkGray, canUseColors);
            WriteLineColored(new string('=', Math.Max(10, title.Length + 8)), ConsoleColor.DarkGray, canUseColors);
        }

        private static void WriteCategoryHeader(
            string category, int total, int passed, int failed, TimeSpan duration, bool canUseColors)
        {
            // Категория
            WriteColored("Category: ", ConsoleColor.Gray, canUseColors);
            WriteLineColored(category, ConsoleColor.White, canUseColors);

            // Статистика категории
            WriteColored("  Total: ", ConsoleColor.Gray, canUseColors);
            Console.Write(total);

            WriteColored("  Passed: ", ConsoleColor.Gray, canUseColors);
            WriteColored(passed.ToString(), ConsoleColor.Green, canUseColors);

            WriteColored("  Failed: ", ConsoleColor.Gray, canUseColors);
            WriteColored(failed.ToString(), ConsoleColor.Red, canUseColors);

            WriteColored("  Duration: ", ConsoleColor.Gray, canUseColors);
            WriteLineColored(FormatDuration(duration), ConsoleColor.Cyan, canUseColors);
        }

        private static void WriteKeyValue(string key, string value, ConsoleColor valueColor, bool canUseColors)
        {
            WriteColored($"{key}: ", ConsoleColor.Gray, canUseColors);
            WriteLineColored(value, valueColor, canUseColors);
        }

        private static string FormatDuration(TimeSpan ts)
        {
            // Приятный формат: 12ms, 1.23s, 02:15.123 и т.п.
            if (ts.TotalMilliseconds < 1000)
                return $"{ts.TotalMilliseconds:0}ms";
            if (ts.TotalSeconds < 60)
                return $"{ts.TotalSeconds:0.00}s";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }

        private static void WriteColored(string text, ConsoleColor color, bool canUseColors)
        {
            if (!canUseColors)
            {
                Console.Write(text);
                return;
            }

            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }

        private static void WriteLineColored(string text, ConsoleColor color, bool canUseColors = true)
        {
            if (!canUseColors)
            {
                Console.WriteLine(text);
                return;
            }

            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = prev;
        }
    }
}

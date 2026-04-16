using CustomThreadPool;
using Shared;
using System.Reflection;
using TestFrameworkCore.Attributes;
using TestFrameworkCore.Attributes.TestFrameworkCore.Attributes;
using TestFrameworkCore.Exceptions;
using TestFrameworkCore.Interfaces;
using TestFrameworkCore.Results;

namespace TestRunnerProgram
{
    public class CustomThreadPoolTestRunner
    {
        private readonly List<TestResult> _results = [];

        private TimeSpan _totalDuration;

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        private readonly object _resultsLock = new();
        private readonly Lock _outputLock = new();

        private readonly DynamicThreadPool _threadPool =
            new DynamicThreadPool(2, Environment.ProcessorCount * 2);

        private int _activeTasks = 0;

        private readonly ILogger _logger;

        public CustomThreadPoolTestRunner(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<TestResult> RunTestsInAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return RunTestsInAssembly(assembly);
        }

        public IEnumerable<TestResult> RunTestsInAssembly(Assembly assembly)
        {
            DateTime startTestsTime = DateTime.Now;

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestClassAttribute>().Any());

            foreach (var testClass in testClasses)
            {
                RunTestsInClass(testClass);
            }

            WaitForCompletion();

            DateTime endTestsTime = DateTime.Now;
            _totalDuration = endTestsTime - startTestsTime;

            _logger.PrintSummary(_results, _totalDuration);
            return _results;
        }

        private void RunTestsInClass(Type testClass)
        {
            var testMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes<TestMethodAttribute>().Any())
                .Where(m => !m.GetCustomAttributes<SkipTestAttribute>().Any())
                .ToList();

            var testObject = Activator.CreateInstance(testClass);
            if (testObject == null) return;

            var noParallel = testClass.GetCustomAttribute<NoParallelAttribute>() != null;

            var sharedContextAttr = testClass.GetCustomAttribute<SharedContextAttribute>();
            ISharedContext? sharedContext = null;

            if (sharedContextAttr != null)
                sharedContext = ExecuteBeforeAllWithContext(testClass, testObject, sharedContextAttr);

            if (noParallel)
            {
                foreach (var method in testMethods)
                {
                    ExecuteAndStore(testClass, testObject, method, sharedContext);
                }
            }
            else
            {
                foreach (var method in testMethods)
                {
                    var testCases = method.GetCustomAttributes<TestCaseAttribute>().ToList();

                    if (testCases.Count == 0)
                    {
                        EnqueueTest(testClass, testObject, method, sharedContext, null, null);
                    }
                    else
                    {
                        foreach (var tc in testCases)
                        {
                            EnqueueTest(testClass, testObject, method, sharedContext, tc.Parameters, tc.Name);
                        }
                    }
                }
            }

            ExecuteLifecycleMethods(testClass, testObject, typeof(AfterAllAttribute), sharedContext);
            sharedContext?.Dispose();
        }

        private void EnqueueTest(
            Type testClass,
            object testObject,
            MethodInfo method,
            ISharedContext? sharedContext,
            object[]? parameters,
            string? name)
        {
            Interlocked.Increment(ref _activeTasks);

            _threadPool.Enqueue(() =>
            {
                TestResult result;

                try
                {
                    result = ExecuteWithTimeout(testClass, testObject, method, sharedContext, parameters, name);
                }
                catch (TimeoutException)
                {
                    var currentTestName = $"{testClass.Name}.{method.Name}";
                    result = new TestResult()
                    {
                        Passed = false,
                        ErrorMessage = $"\n\t{currentTestName} FAILED: Timeout exceeded\n"
                    };
                }

                lock (_resultsLock)
                {
                    _results.Add(result);
                }

                _logger.PrintSingleResult(result);

                Interlocked.Decrement(ref _activeTasks);
            });
        }

        private void ExecuteAndStore(
            Type testClass,
            object testObject,
            MethodInfo method,
            ISharedContext? sharedContext)
        {
            var result = ExecuteWithTimeout(testClass, testObject, method, sharedContext, null, null);

            lock (_resultsLock)
            {
                _results.Add(result);
            }

            _logger.PrintSingleResult(result);
        }

        private TestResult ExecuteWithTimeout(
            Type testClass,
            object testObject,
            MethodInfo method,
            ISharedContext? sharedContext,
            object[]? parameters,
            string? name)
        {
            TestResult? result = null;

            var thread = new Thread(() =>
            {
                result = ExecuteTestCase(testClass, testObject, method, sharedContext, parameters, name);
            });

            thread.Start();

            var timeout = method.GetCustomAttribute<TimeoutAttribute>()?.Milliseconds;

            if (timeout != null)
            {
                if (!thread.Join(timeout.Value))
                {
                    try { thread.Interrupt(); } catch { }

                    return new TestResult
                    {
                        Passed = false,
                        ErrorMessage = $"\n\t{testClass.Name}.{method.Name} FAILED: Timeout exceeded\n",
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now
                    };
                }
            }
            else
            {
                thread.Join();
            }

            return result!;
        }

        private TestResult ExecuteTestCase(
            Type testClass,
            object testObject,
            MethodInfo method,
            ISharedContext? sharedContext,
            object[]? parameters,
            string? name)
        {
            var testResult = new TestResult
            {
                StartTime = DateTime.Now,
                Passed = true
            };

            var classAttr = testClass.GetCustomAttribute<TestClassAttribute>();
            if (classAttr != null)
                testResult.Category = classAttr.Category ?? "Uncategorized";

            string currentTestName =
                parameters != null
                ? $"{testClass.Name}.{method.Name}[{name ?? string.Join(",", parameters)}]"
                : method.Name;

            try
            {
                ExecuteLifecycleMethods(testClass, testObject, typeof(BeforeEachAttribute), sharedContext);

                ExecuteMethod(testObject, method, parameters);

                testResult.TestName += $"\n\t{currentTestName}\n";
            }
            catch (TargetInvocationException ex) when (ex.InnerException is AssertionFailedException)
            {
                testResult.Passed = false;
                testResult.ErrorMessage +=
                    $"\n\t{currentTestName} Assertion failed: {ex.InnerException.Message}\n";
            }
            catch (Exception ex)
            {
                testResult.Passed = false;
                testResult.ErrorMessage +=
                    $"\n\t{currentTestName} Test failed with exception: {ex.InnerException?.Message ?? ex.Message}\n";
            }
            finally
            {
                ExecuteLifecycleMethods(testClass, testObject, typeof(AfterEachAttribute), sharedContext);

                testResult.EndTime = DateTime.Now;
                testResult.Duration = testResult.EndTime - testResult.StartTime;
            }

            return testResult;
        }

        private void WaitForCompletion()
        {
            while (_activeTasks > 0)
            {
                Thread.Sleep(50);
            }
        }

        private static void ExecuteMethod(object testObject, MethodInfo method, object[]? parameters)
        {
            object[]? normalizedParameters = null;

            if (parameters != null)
            {
                var parametersCount = method.GetParameters().Length;
                normalizedParameters = new object[parametersCount];
                Array.Copy(parameters, normalizedParameters, parametersCount);
            }

            if (method.ReturnType == typeof(Task))
            {
                var task = (Task?)method.Invoke(testObject, normalizedParameters);
                task?.Wait();
            }
            else
            {
                method.Invoke(testObject, normalizedParameters);
            }
        }

        private static void ExecuteLifecycleMethods(
            Type testClass,
            object testObject,
            Type attributeType,
            ISharedContext? sharedContext)
        {
            var lifecycleMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes(attributeType).Any());

            foreach (var method in lifecycleMethods)
            {
                var parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    method.Invoke(testObject, null);
                }
                else if (parameters.Length == 1 && sharedContext != null)
                {
                    method.Invoke(testObject, new object[] { sharedContext });
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Lifecycle method {method.Name} has unsupported parameters.");
                }
            }
        }

        private ISharedContext ExecuteBeforeAllWithContext(
            Type testClass,
            object testObject,
            SharedContextAttribute sharedContextAttribute)
        {
            var lifecycleMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes<BeforeAllAttribute>().Any());

            Type sharedContextType = sharedContextAttribute.ContextType;

            ISharedContext sharedContext = Activator.CreateInstance(sharedContextType) as ISharedContext
                ?? throw new Exception();

            sharedContext.Initialize();

            foreach (var method in lifecycleMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1)
                    method.Invoke(testObject, [sharedContext]);
                else
                    method.Invoke(testObject, null);
            }

            return sharedContext;
        }
        /*public void PrintResults()
        {
            if (_results == null) throw new ArgumentNullException(nameof(_results));

            if (_results.Count == 0)
            {
                WriteLineColored("No test results.", ConsoleColor.DarkYellow);
                return;
            }

            bool canUseColors = !Console.IsOutputRedirected;

            int total = _results.Count;
            int passed = _results.Count(r => r.Passed);
            int failed = total - passed;
            TimeSpan totalDuration = new TimeSpan(_results.Sum(r => r.Duration.Ticks));

            WriteHeader("TEST RUN SUMMARY", canUseColors);
            WriteKeyValue("Total", total.ToString(), ConsoleColor.Gray, canUseColors);
            WriteKeyValue("Passed", passed.ToString(), ConsoleColor.Green, canUseColors);
            WriteKeyValue("Failed", failed.ToString(), ConsoleColor.Red, canUseColors);
            WriteKeyValue("Duration", FormatDuration(_totalDuration), ConsoleColor.Cyan, canUseColors);
            Console.WriteLine();

            var groups = _results
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Category) ? "Uncategorized" : r.Category.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var group in groups)
            {
                var items = group
                    .OrderBy(r => r.Passed)
                    .ThenByDescending(r => r.Priority)
                    .ThenBy(r => r.TestName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                int gTotal = items.Length;
                int gPassed = items.Count(r => r.Passed);
                int gFailed = gTotal - gPassed;
                var gDuration = new TimeSpan(items.Sum(r => r.Duration.Ticks));

                WriteCategoryHeader(group.Key, gTotal, gPassed, gFailed, gDuration, canUseColors);

                foreach (var r in items)
                {
                    var statusText = r.Passed ? "PASS" : "FAIL";
                    var statusColor = r.Passed ? ConsoleColor.Green : ConsoleColor.Red;

                    WriteColored($"  [{statusText}]", statusColor, canUseColors);
                    Console.Write(" ");
                    Console.Write(r.TestName);
                    Console.Write($"  (P{r.Priority})");
                    Console.Write($"  {FormatDuration(r.Duration)}");
                    Console.Write($"  {r.StartTime:HH:mm:ss}–{r.EndTime:HH:mm:ss}");
                    Console.WriteLine();

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
            WriteColored("Category: ", ConsoleColor.Gray, canUseColors);
            WriteLineColored(category, ConsoleColor.White, canUseColors);

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
        public void PrintSingleResult(TestResult r)
        {
            lock (_outputLock)
            {
                if (r == null) throw new ArgumentNullException(nameof(r));

                bool canUseColors = !Console.IsOutputRedirected;

                var statusText = r.Passed ? "PASS" : "FAIL";
                var statusColor = r.Passed ? ConsoleColor.Green : ConsoleColor.Red;

                WriteColored($"  [{statusText}]", statusColor, canUseColors);
                Console.Write(" ");
                Console.Write(r.TestName);
                if (r.Passed)
                {
                    Console.Write($"  {FormatDuration(r.Duration)}");
                    Console.Write($"  {r.StartTime:HH:mm:ss}–{r.EndTime:HH:mm:ss}");
                }
                Console.WriteLine();

                if (!r.Passed && !string.IsNullOrWhiteSpace(r.ErrorMessage))
                {
                    WriteColored("      ", ConsoleColor.DarkRed, canUseColors);
                    WriteLineColored(r.ErrorMessage.Trim(), ConsoleColor.DarkRed, canUseColors);
                }
            }
        }

        public void PrintSummary(IReadOnlyCollection<TestResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            bool canUseColors = !Console.IsOutputRedirected;

            if (results.Count == 0)
            {
                WriteLineColored("No test results.", ConsoleColor.DarkYellow, canUseColors);
                return;
            }

            int total = results.Count;
            int passed = results.Count(r => r.Passed);
            int failed = total - passed;

            WriteHeader("TEST RUN SUMMARY", canUseColors);
            WriteKeyValue("Total", total.ToString(), ConsoleColor.Gray, canUseColors);
            WriteKeyValue("Passed", passed.ToString(), ConsoleColor.Green, canUseColors);
            WriteKeyValue("Failed", failed.ToString(), ConsoleColor.Red, canUseColors);
            WriteKeyValue("Duration", FormatDuration(_totalDuration), ConsoleColor.Cyan, canUseColors);

            Console.WriteLine();
        }*/
    }
}
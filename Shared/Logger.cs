using TestFrameworkCore.Results;

namespace Shared
{
    public class ConsoleLogger : ILogger
    {
        private static readonly object _lock = new object();

        public void Log(string message)
        {
            lock (_lock)
            {
                Console.WriteLine(message);
            }
        }

        private void WriteLineColored(string text, ConsoleColor color, bool canUseColors = true)
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

        private void WriteColored(string text, ConsoleColor color, bool canUseColors)
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

        private void WriteHeader(string title, bool canUseColors)
        {
            WriteLineColored(new string('=', Math.Max(10, title.Length + 8)), ConsoleColor.DarkGray);
            WriteColored("=== ", ConsoleColor.DarkGray, canUseColors);
            WriteColored(title, ConsoleColor.White, canUseColors);
            WriteLineColored(" ===", ConsoleColor.DarkGray, canUseColors);
            WriteLineColored(new string('=', Math.Max(10, title.Length + 8)), ConsoleColor.DarkGray, canUseColors);
        }

        private void WriteCategoryHeader(
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

        private void WriteKeyValue(string key, string value, ConsoleColor valueColor, bool canUseColors)
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

        public void PrintSingleResult(TestResult r)
        {
            lock (_lock)
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

        public void PrintSummary(IReadOnlyCollection<TestResult> results, TimeSpan totalDuration)
        {
            lock (_lock)
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
                WriteKeyValue("Duration", FormatDuration(totalDuration), ConsoleColor.Cyan, canUseColors);

                Console.WriteLine();
            }
        }

        public void PrintResults(IReadOnlyCollection<TestResult> results, TimeSpan totalDuration)
        {
            lock (_lock)
            {
                if (results == null) throw new ArgumentNullException(nameof(results));

                if (results.Count == 0)
                {
                    WriteLineColored("No test results.", ConsoleColor.DarkYellow);
                    return;
                }

                bool canUseColors = !Console.IsOutputRedirected;

                int total = results.Count;
                int passed = results.Count(r => r.Passed);
                int failed = total - passed;

                WriteHeader("TEST RUN SUMMARY", canUseColors);
                WriteKeyValue("Total", total.ToString(), ConsoleColor.Gray, canUseColors);
                WriteKeyValue("Passed", passed.ToString(), ConsoleColor.Green, canUseColors);
                WriteKeyValue("Failed", failed.ToString(), ConsoleColor.Red, canUseColors);
                WriteKeyValue("Duration", FormatDuration(totalDuration), ConsoleColor.Cyan, canUseColors);
                Console.WriteLine();

                var groups = results
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
        }
    }
}

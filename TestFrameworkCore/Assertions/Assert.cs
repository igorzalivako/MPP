using System.Linq.Expressions;
using TestFrameworkCore.Exceptions;

namespace TestFrameworkCore.Assertions
{
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = "Expected true")
        {
            if (!condition)
                throw new AssertionFailedException($"Assert.IsTrue failed: {message}");
        }

        public static void IsFalse(bool condition, string message = "Expected false")
        {
            if (condition)
                throw new AssertionFailedException($"Assert.IsFalse failed: {message}");
        }

        public static void AreEqual(object expected, object? actual, string? message = null)
        {
            if (!expected.Equals(actual))
                throw new AssertionFailedException($"Assert.AreEqual failed: Expected '{expected}', Actual '{actual}'. {message}");
        }

        public static void AreNotEqual(object expected, object actual, string? message = null)
        {
            if (expected.Equals(actual))
                throw new AssertionFailedException($"Assert.AreNotEqual failed: Values are equal '{expected}'. {message}");
        }

        public static void IsNull(object? obj, string message = "Expected null")
        {
            if (obj != null)
                throw new AssertionFailedException($"Assert.IsNull failed: {message}");
        }

        public static void IsNotNull(object? obj, string message = "Expected not null")
        {
            if (obj == null)
                throw new AssertionFailedException($"Assert.IsNotNull failed: {message}");
        }

        public static void Throws<TException>(Action action, string? message = null) where TException : Exception
        {
            try
            {
                action();
                throw new AssertionFailedException($"Assert.Throws failed: Expected exception of type {typeof(TException)} but no exception was thrown. {message}");
            }
            catch (TException)
            {
                // Ожидаемое исключение
            }
            catch (Exception ex)
            {
                throw new AssertionFailedException($"Assert.Throws failed: Expected exception of type {typeof(TException)} but caught {ex.GetType()}. {message}");
            }
        }

        public static async Task ThrowsAsync<TException>(Func<Task> asyncAction, string? message = null) where TException : Exception
        {
            Task task = asyncAction.Invoke();
            try
            {
                await task;
                throw new AssertionFailedException($"Assert.ThrowsAsync failed: Expected exception of type {typeof(TException)} but no exception was thrown. {message}");
            }
            catch
            {
                if (task.Exception?.InnerException is not TException)
                    throw new AssertionFailedException($"Assert.ThrowsAsync failed: Expected exception of type {typeof(TException)} but caught {task.Exception?.InnerException?.GetType()}. {message}");
            }
        }

        public static void DoesNotThrow(Action action, string? message = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new AssertionFailedException($"Assert.DoesNotThrow failed: Unexpected exception of type {ex.GetType()}. {message}");
            }
        }

        public static void InRange(int value, int min, int max, string? message = null)
        {
            if (value < min || value > max)
                throw new AssertionFailedException($"Assert.InRange failed: Value {value} is not in range [{min}, {max}]. {message}");
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (!collection.Contains(item))
                throw new AssertionFailedException($"Assert.Contains failed: Collection does not contain '{item}'. {message}");
        }

        public static void That(Expression<Func<bool>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            bool result;
            try
            {
                result = expression.Compile().Invoke();
            }
            catch (Exception ex)
            {
                throw new AssertionFailedException(
                    $"Assert.That failed during evaluation: {ex.Message}");
            }

            if (result) return;

            var analysis = Analyze(expression.Body);

            var message =
                $"Assert.That failed:\n" +
                $"Expression: {expression.Body}\n" +
                $"Details:\n{analysis}";

            throw new AssertionFailedException(message);
        }

        private static string Analyze(Expression expr, int indent = 0)
        {
            string pad = new string(' ', indent * 2);

            switch (expr)
            {
                case BinaryExpression bin:
                    return AnalyzeBinary(bin, indent);

                case MethodCallExpression call:
                    return AnalyzeMethodCall(call, indent);

                case MemberExpression member:
                    return $"{pad}{expr} = {SafeEval(expr)}";

                case ConstantExpression constant:
                    return $"{pad}{constant.Value}";

                case UnaryExpression unary:
                    return AnalyzeUnary(unary, indent);

                default:
                    return $"{pad}{expr} = {SafeEval(expr)}";
            }
        }

        private static string AnalyzeBinary(BinaryExpression bin, int indent)
        {
            string pad = new string(' ', indent * 2);

            var leftVal = SafeEval(bin.Left);
            var rightVal = SafeEval(bin.Right);
            var result = SafeEval(bin);

            var op = GetOperator(bin.NodeType);

            return
                $"{pad}{bin.NodeType} ({op}) => {result}\n" +
                $"{pad}LEFT:\n{Analyze(bin.Left, indent + 1)}\n" +
                $"{pad}RIGHT:\n{Analyze(bin.Right, indent + 1)}\n" +
                $"{pad}VALUES: {leftVal} {op} {rightVal}";
        }

        private static string AnalyzeMethodCall(MethodCallExpression call, int indent)
        {
            string pad = new string(' ', indent * 2);

            var args = call.Arguments
                .Select(a => $"{a} = {SafeEval(a)}")
                .ToArray();

            var result = SafeEval(call);

            return
                $"{pad}CALL: {call.Method.Name} => {result}\n" +
                $"{pad}ARGS:\n{string.Join("\n", args.Select(a => pad + "  " + a))}";
        }

        private static string AnalyzeUnary(UnaryExpression unary, int indent)
        {
            string pad = new string(' ', indent * 2);

            var operandVal = SafeEval(unary.Operand);
            var result = SafeEval(unary);

            return
                $"{pad}{unary.NodeType} => {result}\n" +
                $"{pad}OPERAND:\n{Analyze(unary.Operand, indent + 1)}\n" +
                $"{pad}VALUE: {operandVal}";
        }

        private static object? SafeEval(Expression expr)
        {
            try
            {
                var lambda = Expression.Lambda(expr);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
            catch
            {
                return "<?>"; // если не удалось вычислить
            }
        }

        private static string GetOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "&&",
                ExpressionType.OrElse => "||",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                _ => type.ToString()
            };
        }
    }
}
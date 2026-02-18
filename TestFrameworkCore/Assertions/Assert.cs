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
            if (!Equals(expected, actual))
                throw new AssertionFailedException($"Assert.AreEqual failed: Expected '{expected}', Actual '{actual}'. {message}");
        }

        public static void AreNotEqual(object expected, object actual, string? message = null)
        {
            if (Equals(expected, actual))
                throw new AssertionFailedException($"Assert.AreNotEqual failed: Values are equal '{expected}'. {message}");
        }

        public static void IsNull(object? obj, string message = "Expected null")
        {
            if (obj != null)
                throw new AssertionFailedException($"Assert.IsNull failed: {message}");
        }

        public static void IsNotNull(object obj, string message = "Expected not null")
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
    }

}

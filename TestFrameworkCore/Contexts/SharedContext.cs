namespace TestFrameworkCore.Contexts
{
    public class SharedContext : IDisposable
    {
        private static SharedContext? _current;
        
        private Dictionary<string, object> _sharedData = [];
        
        public int InitializationCount { get; private set; }
        
        public bool IsDisposed { get; private set; }

        private SharedContext()
        {
            InitializationCount++;
            Console.WriteLine("SharedContext initialized");
        }

        public void SetData(string key, object value)
        {
            _sharedData[key] = value;
        }

        public T? GetData<T>(string key)
        {
            return _sharedData.TryGetValue(key, out object? value) ? (T)value : default;
        }

        public void Dispose()
        {
            IsDisposed = true;
            _sharedData.Clear();
            Console.WriteLine("SharedContext disposed");
        }

        public static SharedContext Create()
        {
            if (_current is null || _current.IsDisposed)
            {
                _current = new SharedContext();
            }
            return _current;
        }
    }

}

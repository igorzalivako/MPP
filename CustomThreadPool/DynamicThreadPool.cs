using Shared;

namespace CustomThreadPool
{
    public class DynamicThreadPool
    {
        private readonly Queue<WorkItem> _queue = new();
        private readonly List<Worker> _workers = new();

        private readonly object _lock = new();

        private readonly int _minThreads;
        private readonly int _maxThreads;

        private readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(2);

        private int _activeTasks = 0;
        private int _workersToStop = 0;

        private ILogger _logger = new ConsoleLogger();

        public object SyncRoot => _lock;

        public bool HasWork => _queue.Count > 0;

        public TimeSpan IdleTimeout => _idleTimeout;

        public DynamicThreadPool(int minThreads, int maxThreads)
        {
            _minThreads = minThreads;
            _maxThreads = maxThreads;

            for (int i = 0; i < _minThreads; i++)
                AddWorker();
        }

        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _queue.Enqueue(new WorkItem(action));
                Log($"Task enqueued | Queue: {_queue.Count}");

                Monitor.Pulse(_lock);
            }

            ScaleUpIfNeeded();
        }

        public WorkItem DequeueUnsafe()
        {
            return _queue.Dequeue();
        }

        private void AddWorker()
        {
            var worker = new Worker(this);

            lock (_lock)
            {
                _workers.Add(worker);
                Log($"Worker created | Total workers: {_workers.Count}");
            }

            worker.Start();
        }

        public void RemoveWorker(Worker worker)
        {
            lock (_lock)
            {
                _workers.Remove(worker);

                if (_workersToStop > 0)
                    _workersToStop--;

                Log($"Worker removed | Total workers: {_workers.Count}");
            }
        }

        private void ScaleUpIfNeeded()
        {
            lock (_lock)
            {
                if (_queue.Count > _workers.Count && _workers.Count < _maxThreads)
                {
                    Log("Scaling UP");
                    AddWorker();
                }
            }
        }

        public bool TryShrink(Worker worker)
        {
            lock (_lock)
            {
                int aliveWorkers = _workers.Count - _workersToStop;

                if (aliveWorkers <= _minThreads)
                    return false;

                _workersToStop++;
                Log("Scaling DOWN (idle worker)");
            }

            worker.Stop();
            return true;
        }

        public void WakeAllWorkers()
        {
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }
        }

        public void NotifyTaskStart()
        {
            Interlocked.Increment(ref _activeTasks);
        }

        public void NotifyTaskEnd()
        {
            Interlocked.Decrement(ref _activeTasks);

            if (_activeTasks == 0 && _queue.Count == 0)
            {
                lock (_lock)
                {
                    Monitor.PulseAll(_lock);
                }
            }
        }

        public void LogError(Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }

        public void Log(string message)
        {
            _logger.Log($"[POOL] {DateTime.Now:HH:mm:ss} | {message}");
        }

        public int WorkerCount
        {
            get
            {
                lock (_lock) return _workers.Count;
            }
        }

        public int QueueLength
        {
            get
            {
                lock (_lock) return _queue.Count;
            }
        }

        public int ActiveTasks => _activeTasks;
    }
}
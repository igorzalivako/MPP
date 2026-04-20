namespace CustomThreadPool
{
    public class ThreadPoolEvents
    {
        public event Action? WorkerCreated;
        public event Action? WorkerRemoved;
        public event Action? TaskStarted;
        public event Action? TaskCompleted;

        internal void OnWorkerCreated() => WorkerCreated?.Invoke();
        internal void OnWorkerRemoved() => WorkerRemoved?.Invoke();
        internal void OnTaskStarted() => TaskStarted?.Invoke();
        internal void OnTaskCompleted() => TaskCompleted?.Invoke();
    }
}
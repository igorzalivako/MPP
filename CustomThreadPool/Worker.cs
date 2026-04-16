namespace CustomThreadPool
{
    public class Worker
    {
        private readonly Thread _thread;
        private readonly DynamicThreadPool _pool;
        private volatile bool _running = true;

        public DateTime LastActiveTime { get; private set; }

        public Worker(DynamicThreadPool pool)
        {
            _pool = pool;
            _thread = new Thread(WorkLoop)
            {
                IsBackground = true
            };

            LastActiveTime = DateTime.Now;
        }

        public void Start() => _thread.Start();

        public void Stop()
        {
            _running = false;
            _pool.WakeAllWorkers();
        }

        private void WorkLoop()
        {
            while (_running)
            {
                WorkItem workItem = null;

                lock (_pool.SyncRoot)
                {
                    while (_running && !_pool.HasWork)
                    {
                        bool signaled = Monitor.Wait(_pool.SyncRoot, _pool.IdleTimeout);

                        if (!signaled && !_pool.HasWork)
                        {
                            if (_pool.TryShrink(this))
                                break;
                        }
                    }

                    if (!_running)
                        break;

                    workItem = _pool.DequeueUnsafe();
                }

                try
                {
                    LastActiveTime = DateTime.Now;

                    _pool.NotifyTaskStart();
                    _pool.Log("Task started");

                    workItem.Action();

                    _pool.Log("Task finished");
                }
                catch (Exception ex)
                {
                    _pool.LogError(ex);
                }
                finally
                {
                    _pool.NotifyTaskEnd();
                }
            }

            _pool.RemoveWorker(this);
        }
    }
}
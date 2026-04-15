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

        public void Stop() => _running = false;

        private void WorkLoop()
        {
            while (_running)
            {
                if (_pool.TryDequeue(out var workItem))
                {
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
                else
                {
                    Thread.Sleep(50);
                }

                _pool.CheckForShrink(this);
            }

            _pool.RemoveWorker(this);
        }
    }
}
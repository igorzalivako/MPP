namespace CustomThreadPool
{
    public class WorkItem
    {
        public Action Action { get; }
        public DateTime EnqueueTime { get; }

        public WorkItem(Action action)
        {
            Action = action;
            EnqueueTime = DateTime.Now;
        }
    }
}

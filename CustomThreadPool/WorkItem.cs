namespace CustomThreadPool
{
    public class WorkItem
    {
        public Action Action { get; }

        public WorkItem(Action action)
        {
            Action = action;
        }
    }
}

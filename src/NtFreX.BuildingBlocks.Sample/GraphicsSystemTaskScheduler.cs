using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NtFreX.BuildingBlocks.Sample
{
    class GraphicsSystemTaskScheduler : TaskScheduler
    {
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly int _mainThreadID;

        public GraphicsSystemTaskScheduler(int mainThreadID)
        {
            _mainThreadID = mainThreadID;
        }

        public void FlushQueuedTasks()
        {
            while (_tasks.TryTake(out var t))
            {
                TryExecuteTask(t);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return Environment.CurrentManagedThreadId == _mainThreadID && TryExecuteTask(task);
        }

        public void Shutdown()
        {
            _tasks.CompleteAdding();
        }
    }
}
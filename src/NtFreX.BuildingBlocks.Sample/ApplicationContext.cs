using Microsoft.Extensions.Logging;

namespace NtFreX.BuildingBlocks.Sample
{
    public static class ApplicationContext
    {
        public static bool IsDebug { get; private set; } = false;
        //public static int MainThreadID { get; private set; }
        //public static TaskScheduler GraphicsSystemTaskScheduler { get; private set; }

        public static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(x => x.AddConsole());

        static ApplicationContext()
        {

#if DEBUG
            IsDebug = true;
#endif

            //MainThreadID = Environment.CurrentManagedThreadId;
            //GraphicsSystemTaskScheduler = new GraphicsSystemTaskScheduler(MainThreadID);
        }

        //public static Task<T> ExecuteOnMainThread<T>(Func<T> func)
        //{
        //    if (Environment.CurrentManagedThreadId == MainThreadID)
        //    {
        //        return Task.FromResult(func());
        //    }

        //    return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, GraphicsSystemTaskScheduler);
        //}
        //public static Task ExecuteOnMainThread(Action action)
        //{
        //    if (Environment.CurrentManagedThreadId == MainThreadID)
        //    {
        //        action();
        //        return Task.CompletedTask;
        //    }

        //    return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, GraphicsSystemTaskScheduler);
        //}
    }
}

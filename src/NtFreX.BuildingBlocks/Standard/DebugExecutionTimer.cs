using System.Diagnostics;

namespace NtFreX.BuildingBlocks.Standard
{
    public class DebugExecutionTimer
    {
        public readonly DebugExecutionTimerSource Source;

        private readonly Stopwatch stopwatch = new Stopwatch();

        private long startTime;

        public DebugExecutionTimer(DebugExecutionTimerSource debugExecutionTimerSource)
        { 
            this.Source = debugExecutionTimerSource;
            this.stopwatch.Start();
        }

        public void Start()
        {
            startTime = stopwatch.ElapsedTicks;
        }

        public void Stop()
        {
            this.Source.AddValue(this.stopwatch.ElapsedTicks - startTime);
        }
    }
}

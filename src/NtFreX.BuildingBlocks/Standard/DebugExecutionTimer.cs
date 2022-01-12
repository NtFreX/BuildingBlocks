using System.Diagnostics;

namespace NtFreX.BuildingBlocks.Standard
{
    public class DebugExecutionTimer : IDisposable
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly DebugExecutionTimerSource debugExecutionTimerSource;

        public DebugExecutionTimer(DebugExecutionTimerSource debugExecutionTimerSource)
        { 
            this.debugExecutionTimerSource = debugExecutionTimerSource;
            this.stopwatch.Start();
        }

        public void Dispose()
        {
            this.stopwatch.Stop();
            this.debugExecutionTimerSource.AddValue(this.stopwatch.ElapsedTicks);
        }
    }
}

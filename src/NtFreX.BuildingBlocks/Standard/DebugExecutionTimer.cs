using System.Diagnostics;

namespace NtFreX.BuildingBlocks.Standard;

public class DebugExecutionTimer
{
    public readonly DebugExecutionTimerSource Source;
    public readonly Stopwatch Stopwatch;

    private long startTime;

    public DebugExecutionTimer(DebugExecutionTimerSource debugExecutionTimerSource, Stopwatch stopwatch)
    { 
        Source = debugExecutionTimerSource;
        Stopwatch = stopwatch;
    }

    public void Start()
        => startTime = Stopwatch.ElapsedTicks;

    public void Stop()
        => Source.AddValue(Stopwatch.ElapsedTicks - startTime);
}
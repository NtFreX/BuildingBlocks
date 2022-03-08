using Microsoft.Extensions.Logging;

namespace NtFreX.BuildingBlocks.Standard;

public class DebugExecutionTimerSource
{
    private readonly object locker = new ();
    private readonly ILogger<DebugExecutionTimerSource> logger;
    private readonly int bucketSize;

    private long sum;
    private int bucketIndex = 0;

    public readonly string Name;

    public float AverageMilliseconds { get; private set; }

    public DebugExecutionTimerSource(ILogger<DebugExecutionTimerSource> logger, string name, int bucketSize = 100)
    {
        Name = name;

        this.logger = logger;
        this.bucketSize = bucketSize;
    }

    internal void AddValue(long ticks)
    {
        lock (locker) // TODO: do we need that lock???
        {
            sum += ticks;
            if (++bucketIndex == bucketSize)
            {
                AverageMilliseconds = sum * 1f / bucketSize / 10_000;

                logger.LogInformation($"Execution of {Name} took {AverageMilliseconds}ms");

                sum = 0;
                bucketIndex = 0;
            }
        }
    }
}
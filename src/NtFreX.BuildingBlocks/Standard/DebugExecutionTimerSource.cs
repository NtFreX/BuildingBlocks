using Microsoft.Extensions.Logging;

namespace NtFreX.BuildingBlocks.Standard
{
    public class DebugExecutionTimerSource
    {
        private readonly object locker = new object();
        private readonly ILogger<DebugExecutionTimerSource> logger;
        private readonly string name;
        private long[] bucket;
        private int bucketIndex = 0;

        public TimeSpan Average => TimeSpan.FromTicks((long)bucket.Average());

        public DebugExecutionTimerSource(ILogger<DebugExecutionTimerSource> logger, string name, int bucketSize = 100)
        {
            this.logger = logger;
            this.name = name;
            this.bucket = new long[bucketSize];
        }

        internal void AddValue(long value)
        {
            lock (locker)
            {
                bucket[bucketIndex++] = value;
                if(bucketIndex == bucket.Length)
                {
                    logger.LogInformation($"Execution of {name} took {Average.TotalMilliseconds}ms");
                    bucketIndex = 0;
                }
            }
        }
    }
}

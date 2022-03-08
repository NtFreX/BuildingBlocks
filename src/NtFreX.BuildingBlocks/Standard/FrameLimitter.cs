namespace NtFreX.BuildingBlocks.Standard;

//TODO: this leads to a very blocky rendering (once good fps and then bad) fix it!
public class FrameLimitter : IFrameLimitter
{
    private readonly float minDelta;

    private int lastDelay;
    private float lastDeltaAfterDelay;

    public FrameLimitter(float maxFps)
    {
        minDelta = 1000 / maxFps;
    }

    public Task LimitAsync(float delta)
    {
        if (lastDelay > 0)
        {
            lastDeltaAfterDelay = delta;
            lastDelay = 0;
        }

        var delay = (int) (minDelta - delta - (lastDeltaAfterDelay == 0 ? 0 : lastDeltaAfterDelay > minDelta ? lastDeltaAfterDelay - minDelta : minDelta - lastDeltaAfterDelay));
        lastDeltaAfterDelay = 0;

        if (delay > 0)
        {
            lastDelay = delay;
            return Task.Delay(delay);
        }
        return Task.CompletedTask;
    }
}

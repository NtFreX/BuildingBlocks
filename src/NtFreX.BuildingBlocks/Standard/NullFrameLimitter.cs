namespace NtFreX.BuildingBlocks.Standard;

public class NullFrameLimitter : IFrameLimitter
{
    public Task LimitAsync(float delta) => Task.CompletedTask;
}

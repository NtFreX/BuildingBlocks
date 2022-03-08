namespace NtFreX.BuildingBlocks.Standard;

public interface IFrameLimitter
{
    Task LimitAsync(float delta);
}

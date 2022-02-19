namespace NtFreX.BuildingBlocks.Audio;

public interface IAudioContext
{
    public bool Loop { get; set; }
    public int Volume { get; set; }
    public bool IsPaused { get; set; }
    public bool IsStopped { get; set; }
}

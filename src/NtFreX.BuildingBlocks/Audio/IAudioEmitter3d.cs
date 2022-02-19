using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio;

public interface IAudioEmitter3d
{
    public Vector3 Position { get; }
    public float Intensity { get; }
    public int Volume { get; }

    void TogglePlayPause();
    void Update(Vector3? listenerPosition);
}

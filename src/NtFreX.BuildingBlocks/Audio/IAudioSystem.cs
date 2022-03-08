using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio;

public interface IAudioSystem : IDisposable
{
    void StopAll();
    void PreLoadWav(string file);
    void Update(Vector3? listenerPosition);
    IAudioContext PlayWav(string file, int volume = 128, bool loop = false);
    IAudioEmitter3d PlaceWav(string file, Vector3 listenerPosition, Vector3 position, float intensity, int volume = 128, bool loop = false);
}

using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio.Sdl2;

public class SdlAudioEmitter3d : IAudioEmitter3d
{
    private readonly SdlAudioContext audioContext;

    public Vector3 Position { get; }
    public float Intensity { get; }
    public int Volume { get; }

    public bool IsStopped => audioContext.IsStopped;

    public SdlAudioEmitter3d(SdlAudioContext audioContext, Vector3 position, int volume, float intensity)
    {
        this.audioContext = audioContext;

        Position = position;
        Intensity = intensity;
        Volume = volume;
    }

    internal static SdlAudioEmitter3d Create(SdlAudioRenderer sdlAudioRenderer, SdlAudioFile audioFile, Vector3 position, Vector3? listenerPosition, float intensity, int volume = 128, bool loop = false)
        => new SdlAudioEmitter3d(sdlAudioRenderer.PlayWav(audioFile, GetAudioVolume(position, listenerPosition, volume, intensity), loop), position, volume, intensity);

    public void TogglePlayPause() => audioContext.IsPaused = !audioContext.IsPaused;

    public void Update(Vector3? listenerPosition)
    {
        audioContext.Volume = GetAudioVolume(Position, listenerPosition, Volume, Intensity);
    }

    private static int GetAudioVolume(Vector3 position, Vector3? listenerPosition, int volume, float intensity)
    {
        if (listenerPosition == null)
            return 0;

        var halfDistanceFactor = Math.Max(.5f, Vector3.Distance(listenerPosition.Value, position) / intensity);
        return (int)(volume / (halfDistanceFactor * 2f));
    }
}

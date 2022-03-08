using NtFreX.BuildingBlocks.Standard;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio.Sdl2;

public sealed class SdlAudioSystem : IAudioSystem
{
    private readonly Dictionary<string, SdlAudioFile> audioCache = new ();
    private readonly SdlAudioRenderer audioRenderer = new();
    private readonly ConcurrentBox<SdlAudioEmitter3d> soundEmitters = new();

    private SdlAudioFile GetCachedAudio(string file)
    {
        if (!audioCache.TryGetValue(file, out var audio))
        {
            audio = new SdlAudioFile(file);
            audioCache.Add(file, audio);
        }
        return audio;
    }
        
    public void StopAll()
    {
        audioRenderer.StopAll();
    }

    public void PreLoadWav(string file)
    {
        GetCachedAudio(file);
    }

    public IAudioContext PlayWav(string file, int volume = 128, bool loop = false)
    {
        var audio = GetCachedAudio(file);
        return audioRenderer.PlayWav(audio, volume, loop);
    }

    public IAudioEmitter3d PlaceWav(string file, Vector3 listenerPosition, Vector3 position, float intensity, int volume = 128, bool loop = false)
    {
        var audio = GetCachedAudio(file);
        var emitter = SdlAudioEmitter3d.Create(audioRenderer, audio, position, listenerPosition, intensity, volume, loop);
        soundEmitters.Add(emitter);
        return emitter;
    }

    public void Update(Vector3? listenerPosition)
    {
        for(var i = 0; i < soundEmitters.Count(); i++)
        {
            var item = soundEmitters.Get(i);
            if (item.IsDead)
                continue;

            var emitter = item.Value;
            if (emitter.IsStopped)
            {
                soundEmitters.Kill(i);
            }
            else
            {
                emitter.Update(listenerPosition);
            }
        }

        soundEmitters.Cleanup();
    }

    public void Dispose()
    {
        audioRenderer.Dispose();
        foreach(var item in audioCache.Values)
        {
            item.Dispose();
        }
    }
}

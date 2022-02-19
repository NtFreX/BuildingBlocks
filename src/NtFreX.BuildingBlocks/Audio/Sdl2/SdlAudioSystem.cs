using NtFreX.BuildingBlocks.Model;
using System.Collections.Concurrent;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio.Sdl2;

public class SdlAudioSystem : IAudioSystem
{
    private readonly Dictionary<string, SdlAudioFile> audioCache = new ();
    private readonly SdlAudioRenderer audioRenderer = new();
    private readonly ConcurrentDictionary<SdlAudioEmitter3d, object?> soundEmitters = new();
    private readonly GraphicsSystem graphicsSystem;

    public SdlAudioSystem(GraphicsSystem graphicsSystem)
    {
        this.graphicsSystem = graphicsSystem;
    }

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

    public IAudioEmitter3d PlaceWav(string file, Vector3 position, float intensity, int volume = 128, bool loop = false)
    {
        var audio = GetCachedAudio(file);
        var emitter = SdlAudioEmitter3d.Create(audioRenderer, audio, position, graphicsSystem.Camera.Value?.Position.Value, intensity, volume, loop);
        soundEmitters.TryAdd(emitter, null);
        return emitter;
    }

    public void Update(Vector3? listenerPosition)
    {
        foreach ((var emitter, _) in soundEmitters)
        {
            if (emitter.IsStopped)
            {
                soundEmitters.TryRemove(emitter, out _);
            }
            else
            {
                emitter.Update(listenerPosition);
            }
        }
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

using System;
using System.Collections.Generic;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio
{
    public class SdlAudioSystem : IDisposable
    {
        private readonly Dictionary<string, SdlAudioFile> audioCache = new Dictionary<string, SdlAudioFile>();
        private readonly object soundEmitterLock = new object();
        private readonly List<SdlAudioEmitter3d> soundEmitters = new List<SdlAudioEmitter3d>();
        private readonly SdlAudioRenderer audioRenderer = new SdlAudioRenderer();
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

        public SdlAudioContext PlayWav(string file, int volume = 128, bool loop = false)
        {
            var audio = GetCachedAudio(file);
            return audioRenderer.PlayWav(audio, volume, loop);
        }

        public SdlAudioEmitter3d PlaceWav(string file, Vector3 position, float intensity, int volume = 128, bool loop = false)
        {
            var audio = GetCachedAudio(file);
            var emitter = SdlAudioEmitter3d.Create(audioRenderer, audio, position, graphicsSystem.Camera.Position, intensity, volume, loop);
            lock (soundEmitterLock)
            {
                soundEmitters.Add(emitter);
            }
            return emitter;
        }

        public void Update(Vector3 cameraPosition)
        {
            var removeList = new List<SdlAudioEmitter3d>();
            lock (soundEmitterLock) 
            {
                foreach (var emitter in soundEmitters)
                {
                    if (emitter.IsStopped)
                    {
                        removeList.Add(emitter);
                    }
                    else
                    {
                        emitter.Update(cameraPosition);
                    }
                }
                foreach (var emitter in removeList)
                {
                    soundEmitters.Remove(emitter);
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
}

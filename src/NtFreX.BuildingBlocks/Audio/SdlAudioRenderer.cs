using SDL2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NtFreX.BuildingBlocks.Audio
{
    public class SdlAudioRenderer : IDisposable
    {
        private readonly object audioContextLock = new object();
        private SDL.SDL_AudioSpec? loadedFormat = null;
        private bool isOpen = false;
        private bool isPaused = true;

        // keep this here so it is not garbage collected
        private readonly SDL.SDL_AudioCallback callbackDelegate;
        private readonly List<SdlAudioContext> audioContexts = new List<SdlAudioContext>();

        static SdlAudioRenderer()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
                throw new Exception("Couldn't initialize sdl");
        }
        public SdlAudioRenderer()
        {
            this.callbackDelegate = SDL_AudioCallback;
        }

        public void Dispose()
        {
            StopAll();
            SDL.SDL_CloseAudio();
        }

        public void StopAll()
        {
            lock (audioContextLock)
            {
                foreach (var context in audioContexts)
                {
                    context.IsStopped = true;
                }
            }
        }

        public SdlAudioContext PlayWav(SdlAudioFile audioFile, int volume = SDL.SDL_MIX_MAXVOLUME, bool loop = false)
        {
            if (loadedFormat != null && !AreEqual(audioFile.Spec, loadedFormat.Value))
                throw new Exception($"Can only play one audio format, loaded format = {loadedFormat}, audio format = {audioFile.Spec}");


            var audioContext = new SdlAudioContext(audioFile)
            {
                Loop = loop,
                Volume = volume
            };
            
            if(!isOpen)
            {
                var spec = audioFile.Spec;
                spec.callback = this.callbackDelegate;
                spec.userdata = IntPtr.Zero;

                if (SDL.SDL_OpenAudio(ref spec, IntPtr.Zero) < 0)
                    throw new Exception($"Couldn't open audio: {SDL.SDL_GetError()}");

                loadedFormat = spec;
                isOpen = true;
            }

            lock (audioContextLock)
            {
                audioContexts.Add(audioContext);
            }
            if (isPaused)
            {
                isPaused = false;
                SDL.SDL_PauseAudio(0);
            }            
            return audioContext;
        }

        private bool AreEqual(SDL.SDL_AudioSpec first, SDL.SDL_AudioSpec second)
        {
            // https://wiki.libsdl.org/SDL_AudioFormat
            var firstBits = new BitArray(first.format);
            var secondBits = new BitArray(second.format);

            var firstSampleRate = BitConverter.GetBytes(first.format)[0];
            var secondSampleRate = BitConverter.GetBytes(second.format)[0];

            return firstBits.Get(15) == secondBits.Get(15) /* is signed */ &&
                   firstBits.Get(12) == secondBits.Get(12) /* is big endian */ &&
                   firstBits.Get(8) == secondBits.Get(8) /* is float */ &&
                   firstSampleRate == secondSampleRate &&
                   first.channels == second.channels &&
                   first.freq == second.freq;
        }

        private unsafe void SDL_AudioCallback(IntPtr userdata, IntPtr stream, int len)
        {
            lock (audioContextLock)
            {
                for (var i = 0; i < audioContexts.Count; i++)
                {
                    if (audioContexts[i].RemainingLength == 0 || audioContexts[i].IsStopped)
                    {
                        if (audioContexts[i].Loop && !audioContexts[i].IsStopped)
                        {
                            audioContexts[i].Reset();
                        }
                        else
                        {
                            audioContexts[i].IsStopped = true;
                            audioContexts.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            var buffer = new byte[len];
            fixed (byte* ptr = buffer)
            {
                SDL.SDL_memcpy(stream, new IntPtr(ptr), new IntPtr(len));
            }


            lock (audioContextLock)
            {
                if (audioContexts.Count == 0)
                {
                    SDL.SDL_PauseAudio(1);
                    isPaused = true;
                    return;
                }

               Task.WaitAll(audioContexts.Where(x => !x.IsPaused).Select(context => Task.Run(() =>
               {
                   var contextLen = len > context.RemainingLength ? context.RemainingLength : (uint)len;
                   SDL.SDL_MixAudioFormat(stream, context.AudioPtr, loadedFormat!.Value.format, contextLen, context.Volume);
                   context.AudioPtr += (int)contextLen;
                   context.RemainingLength -= contextLen;
               })).ToArray());
            }
        }

    }
}

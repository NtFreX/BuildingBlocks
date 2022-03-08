using NtFreX.BuildingBlocks.Standard;
using SDL2;
using System.Collections;

namespace NtFreX.BuildingBlocks.Audio.Sdl2;

internal class SdlAudioRenderer : IDisposable
{
    private readonly object audioContextLock = new ();
    private SDL.SDL_AudioSpec? loadedFormat = null;
    private bool isOpen = false;
    private bool isPaused = true;

    // keep this here so it is not garbage collected
    private readonly SDL.SDL_AudioCallback callbackDelegate;
    private readonly ConcurrentBox<SdlAudioContext> audioContexts = new ();

    static SdlAudioRenderer()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
            throw new Exception("Couldn't initialize sdl");
    }
    public SdlAudioRenderer()
    {
        callbackDelegate = SDL_AudioCallback;
    }

    public void Dispose()
    {
        StopAll();
        SDL.SDL_CloseAudio();
    }

    public void StopAll()
    {
        for(var index = 0; index < audioContexts.Count(); index++)
        {
            audioContexts.Get(index).Value.IsStopped = true;
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

        audioContexts.Add(audioContext);
        if (isPaused)
        {
            isPaused = false;
            SDL.SDL_PauseAudio(0);
        }            
        return audioContext;
    }

    private static bool AreEqual(SDL.SDL_AudioSpec first, SDL.SDL_AudioSpec second)
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
        for (var index = 0; index < audioContexts.Count(); index++)
        {
            var context = audioContexts.Get(index).Value;
            if (context.RemainingLength <= 0 || context.IsStopped)
            {
                if (context.Loop && !context.IsStopped)
                {
                    context.Reset();
                }
                else
                {
                    context.IsStopped = true;
                    audioContexts.Kill(index);
                }
            }
        }

        var buffer = new byte[len];
        fixed (byte* ptr = buffer)
        {
            SDL.SDL_memcpy(stream, new IntPtr(ptr), new IntPtr(len));
        }

        if (audioContexts.Count() == 0)
        {
            SDL.SDL_PauseAudio(1);
            isPaused = true;
            return;
        }

        for (var index = 0; index < audioContexts.Count(); index++)
        {
            var context = audioContexts.Get(index).Value;
            if (context.IsPaused || context.IsStopped)
                return;

            var contextLen = len > context.RemainingLength ? context.RemainingLength : (uint)len;
            if (context.Volume > 0)
            {
                SDL.SDL_MixAudioFormat(stream, context.AudioPtr, loadedFormat!.Value.format, contextLen, context.Volume);
            }
            context.AudioPtr += (int)contextLen;
            context.RemainingLength -= contextLen;
        }
    }

}

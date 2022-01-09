using SDL2;
using System;

namespace NtFreX.BuildingBlocks.Audio
{
    public class SdlAudioFile : IDisposable
    {
        internal IntPtr FilePtr { get; set; }
        internal uint Length { get; set; }
        internal SDL.SDL_AudioSpec Spec { get; set; }

        public SdlAudioFile(string file)
        {
            if (SDL.SDL_LoadWAV(file, out var spec, out var bufferPtr, out var length) == IntPtr.Zero)
                throw new Exception($"Couldn't load the wav file {file}");

            FilePtr = bufferPtr;
            Length = length;
            Spec = spec;
        }

        public void Dispose()
        {
            SDL.SDL_FreeWAV(FilePtr);
        }
    }
}

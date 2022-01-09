using SDL2;
using System;

namespace NtFreX.BuildingBlocks.Audio
{
    public class SdlAudioContext
    {
        private readonly SdlAudioFile audioFile;

        internal IntPtr AudioPtr { get; set; }
        internal uint RemainingLength { get; set; }

        public bool Loop { get; set; }
        public int Volume { get; set; } = SDL.SDL_MIX_MAXVOLUME;
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }

        internal SdlAudioContext(SdlAudioFile audioFile)
        {
            this.AudioPtr = audioFile.FilePtr;
            this.RemainingLength = audioFile.Length;
            this.audioFile = audioFile;
        }

        public void Reset()
        {
            this.AudioPtr = audioFile.FilePtr;
            this.RemainingLength = audioFile.Length;
        }
    }
}

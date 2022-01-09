using System;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Audio
{
    public class SdlAudioEmitter3d
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
            Volume = audioContext.Volume;
        }
        public static SdlAudioEmitter3d Create(SdlAudioRenderer sdlAudioRenderer, SdlAudioFile audioFile, Vector3 position, Vector3 cameraPosition, float intensity, int volume = 128, bool loop = false)
            => new SdlAudioEmitter3d(sdlAudioRenderer.PlayWav(audioFile, GetAudioVolume(position, cameraPosition, volume, intensity), loop), position, volume, intensity);

        public void TogglePlayPause() => audioContext.IsPaused = !audioContext.IsPaused;
        public void Update(Vector3 cameraPosition)
        {
            audioContext.Volume = GetAudioVolume(Position, cameraPosition, Volume, Intensity);
            audioContext.IsPaused = audioContext.Volume == 0;
        }

        private static int GetAudioVolume(Vector3 position, Vector3 cameraPosition, int volume, float intensity)
        {
            var halfDistanceFactor = Math.Max(.5f, Vector3.Distance(cameraPosition, position) / intensity);
            return (int)(volume / (halfDistanceFactor * 2f));
        }
    }
}

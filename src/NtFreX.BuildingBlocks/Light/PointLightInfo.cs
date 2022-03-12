using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Light
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightInfo
    {
        public Vector3 Position;
        private float _padding0;
        public Vector4 Color;
        public float Range; //TODO: are all those paddings needed? (can be reduced by 16 bytes?)
        public float Intensity;
        private float _padding1;
        private float _padding2;
    }
}

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
        private float _padding2;
        private float _padding3;
        private float _padding4;
        public float Intensity;
        private float _padding5;
        private float _padding6;
        private float _padding7;
    }
}

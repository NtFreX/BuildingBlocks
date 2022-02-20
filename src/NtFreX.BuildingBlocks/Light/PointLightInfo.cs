using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Light
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightInfo
    {
        public Vector3 Position;
        public float Range;
        public Vector3 Color;
        public float Intensity;
    }
}

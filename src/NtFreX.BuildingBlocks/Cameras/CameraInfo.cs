using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Desktop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CameraInfo
    {
        public Vector3 CameraPosition;
        public float CameraNearPlaneDistance;

        public Vector3 CameraLookDirection;
        public float CameraFarPlaneDistance;
    }
}

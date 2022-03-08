using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Desktop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CameraInfo
    {
        public Vector3 CameraPosition;
        private float _padding0;
        
        public float CameraNearPlaneDistance;
        private float _padding1;
        private float _padding2;
        private float _padding3;

        public Vector3 CameraLookDirection;
        private float _padding4;

        public float CameraFarPlaneDistance;
        private float _padding5;
        private float _padding6;
        private float _padding7;
    }
}

using System.Numerics;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Models
{
    public struct InstanceInfo
    {
        public static uint Size { get; } = (uint)Unsafe.SizeOf<InstanceInfo>();

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public int TexArrayIndex;

        public InstanceInfo()
        {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            TexArrayIndex = 0;
        }

        public InstanceInfo(Vector3 position, Vector3 rotation, Vector3 scale, int texArrayIndex)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            TexArrayIndex = texArrayIndex;
        }
    }
}

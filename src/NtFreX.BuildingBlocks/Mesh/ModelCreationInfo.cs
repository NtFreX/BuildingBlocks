using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh
{
    public struct ModelCreationInfo
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;
    }
}

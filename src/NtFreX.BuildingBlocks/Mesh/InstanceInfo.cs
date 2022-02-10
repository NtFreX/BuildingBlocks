using System.Numerics;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Mesh
{
    public struct InstanceInfo : IEquatable<InstanceInfo>
    {
        public static InstanceInfo[] Single { get; } = new InstanceInfo[] { new() };
        public static uint Size { get; } = (uint)Unsafe.SizeOf<InstanceInfo>();

        public Vector3 Position { get; init; }
        public Vector3 Rotation { get; init; }
        public Vector3 Scale { get; init; }
        public int TexArrayIndex { get; init; }

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

        public override int GetHashCode() => (Position, Rotation, Scale, TexArrayIndex).GetHashCode();

        public bool Equals(InstanceInfo other)
        {
            return 
                Position == other.Position && 
                Rotation == other.Rotation && 
                Scale == other.Scale && 
                TexArrayIndex == other.TexArrayIndex;
        }

        public override string ToString()
        {
            return $"Postion: {Position}, Rotation: {Rotation}, Scale: {Scale}, TexArrayIndex: {TexArrayIndex}";
        }
    }
}

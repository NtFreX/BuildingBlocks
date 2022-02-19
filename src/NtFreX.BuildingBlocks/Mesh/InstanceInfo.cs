using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public struct InstanceInfo : IEquatable<InstanceInfo>
    {
        public static VertexLayoutDescription VertexLayout => new VertexLayoutDescription(
                new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1)) {  InstanceStepRate = 1 };

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

        public static bool operator !=(InstanceInfo? one, InstanceInfo? two)
            => !(one == two);

        public static bool operator ==(InstanceInfo? one, InstanceInfo? two)
        {
            if (!one.HasValue && !two.HasValue)
                return true;
            if (!one.HasValue)
                return false;
            if (!two.HasValue)
                return false;
            return one.Equals(two);
        }

        public override int GetHashCode() => (Position, Rotation, Scale, TexArrayIndex).GetHashCode();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var objType = obj.GetType();
            if (objType != typeof(InstanceInfo)) return false;
            return Equals((InstanceInfo)obj);
        }

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

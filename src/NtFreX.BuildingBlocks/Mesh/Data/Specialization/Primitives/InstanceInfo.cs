using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;

public struct InstanceInfo : IEquatable<InstanceInfo>
{
    public static VertexLayoutDescription VertexLayout => new (
            new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1)) {  InstanceStepRate = 1 };

    public static InstanceInfo[] Single { get; } = new InstanceInfo[] { new() };
    public static uint Size { get; } = (uint)Unsafe.SizeOf<InstanceInfo>();

    public Vector3 Position { get; init; }
    public Vector3 Rotation { get; init; }
    public Vector3 Scale { get; init; }
    public uint TexArrayIndex { get; init; }

    public InstanceInfo()
    {
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
        TexArrayIndex = 0;
    }

    public InstanceInfo(Vector3 position, Vector3 rotation, Vector3 scale, uint texArrayIndex)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        TexArrayIndex = texArrayIndex;
    }

    public static bool operator !=(InstanceInfo? one, InstanceInfo? two)
        => !(one == two);

    public static bool operator ==(InstanceInfo? one, InstanceInfo? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode() 
        => (Position, Rotation, Scale, TexArrayIndex).GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(InstanceInfo other)
    {
        return 
            Position == other.Position && 
            Rotation == other.Rotation && 
            Scale == other.Scale && 
            TexArrayIndex == other.TexArrayIndex;
    }

    public override string ToString()
        => $"Postion: {Position}, Rotation: {Rotation}, Scale: {Scale}, TexArrayIndex: {TexArrayIndex}";
}
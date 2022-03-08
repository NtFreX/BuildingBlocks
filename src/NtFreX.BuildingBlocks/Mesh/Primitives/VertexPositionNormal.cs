using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct VertexPositionNormal : IVertex, IEquatable<VertexPositionNormal>
{
    //TODO: set correct element semantic
    public static VertexLayoutDescription VertexLayout => new (
        new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription(VertexElementSemantic.Normal.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

    public static ushort BytesBeforePosition => 0;

    public readonly Vector3 Position;
    public readonly Vector3 Normal;

    public VertexPositionNormal(VertexPosition vertex)
        : this(vertex.Position, Vector3.Zero) { }
    public VertexPositionNormal(Vector3 position)
        : this(position, Vector3.Zero) { }
    public VertexPositionNormal(Vector3? position = null, Vector3? normal = null)
    {
        Position = position ?? Vector3.Zero;
        Normal = normal ?? Vector3.Zero;
    }

    public static bool operator !=(VertexPositionNormal? one, VertexPositionNormal? two)
        => !(one == two);

    public static bool operator ==(VertexPositionNormal? one, VertexPositionNormal? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode()
        => Position.GetHashCode();

    public override string ToString()
        => $"Position: {Position}, Normal: {Normal}";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(VertexPositionNormal other)
        => Position == other.Position && Normal == other.Normal;
}


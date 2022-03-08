using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct VertexPositionNormalTexture : IVertex, IEquatable<VertexPositionNormalTexture>
{
    //TODO: set correct element semantic
    public static VertexLayoutDescription VertexLayout => new (
        new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription(VertexElementSemantic.Normal.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription(VertexElementSemantic.TextureCoordinate.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

    public static ushort BytesBeforePosition => 0;

    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Vector2 TextureCoordinate;

    public VertexPositionNormalTexture(VertexPositionNormal vertex)
        : this(vertex.Position, vertex.Normal, Vector2.Zero) { }
    public VertexPositionNormalTexture(VertexPosition vertex)
        : this(vertex.Position, Vector3.Zero, Vector2.Zero) { }
    public VertexPositionNormalTexture(Vector3 position)
        : this(position, Vector3.Zero) { }
    public VertexPositionNormalTexture(Vector3 position, Vector3 normal)
    : this(position, normal, Vector2.Zero) { }
    public VertexPositionNormalTexture(Vector3? position = null, Vector3? normal = null, Vector2? textureCoordinate = null)
    {
        Position = position ?? Vector3.Zero;
        Normal = normal ?? Vector3.Zero;
        TextureCoordinate= textureCoordinate ?? Vector2.Zero;
    }

    public static bool operator !=(VertexPositionNormalTexture? one, VertexPositionNormalTexture? two)
        => !(one == two);

    public static bool operator ==(VertexPositionNormalTexture? one, VertexPositionNormalTexture? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode()
        => Position.GetHashCode();

    public override string ToString()
        => $"Position: {Position}, Normal: {Normal}, TextureCoordinate: {TextureCoordinate}";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(VertexPositionNormalTexture other)
        => Position == other.Position && Normal == other.Normal && TextureCoordinate == other.TextureCoordinate;
}


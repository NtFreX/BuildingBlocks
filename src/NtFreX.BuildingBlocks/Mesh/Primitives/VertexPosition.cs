using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

// TODO: rename to vertexPosition3 and so forth?
public struct VertexPosition : IVertex, IEquatable<VertexPosition>
{
    //TODO: set correct element semantic
    public static VertexLayoutDescription VertexLayout => new (new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
    public static ushort BytesBeforePosition => 0;

    public readonly Vector3 Position;

    public VertexPosition(Vector3? position = null)
    {
        Position = position ?? Vector3.Zero;
    }

    public static bool operator !=(VertexPosition? one, VertexPosition? two)
        => !(one == two);

    public static bool operator ==(VertexPosition? one, VertexPosition? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public static implicit operator VertexPosition(Vector3 position)
        => new (position);

    public static implicit operator Vector3(VertexPosition value)
        => value.Position;

    public override int GetHashCode()
        => Position.GetHashCode();

    public override string ToString()
        => "Position: " + Position.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(VertexPosition other)
        => Position == other.Position;
}


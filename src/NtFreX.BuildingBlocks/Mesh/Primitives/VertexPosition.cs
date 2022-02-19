using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct VertexPosition : IVertex, IEquatable<VertexPosition>
{
    //TODO: set correct element semantic
    public static VertexLayoutDescription VertexLayout => new VertexLayoutDescription(new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
    public static ushort BytesBeforePosition => 0;

    public readonly Vector3 Position;

    public VertexPosition(Vector3? position = null)
    {
        Position = position ?? Vector3.Zero;
    }

    public static bool operator !=(VertexPosition? one, VertexPosition? two)
        => !(one == two);

    public static bool operator ==(VertexPosition? one, VertexPosition? two)
    {
        if (!one.HasValue && !two.HasValue)
            return true;
        if (!one.HasValue)
            return false;
        if (!two.HasValue)
            return false;
        return one.Equals(two);
    }

    public static implicit operator VertexPosition(Vector3 position)
        => new VertexPosition(position);

    public static implicit operator Vector3(VertexPosition value)
        => value.Position;

    public override int GetHashCode()
        => Position.GetHashCode();

    public override string ToString()
        => "Position: " + Position.ToString();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof(VertexPosition)) return false;
        return Equals((VertexPosition)obj);
    }

    public bool Equals(VertexPosition other)
        => Position == other.Position;
}


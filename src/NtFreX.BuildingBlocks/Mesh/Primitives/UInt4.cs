namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct UInt4 : IEquatable<UInt4>
{
    public uint X, Y, Z, W;

    public static bool operator !=(UInt4? one, UInt4? two)
        => !(one == two);

    public static bool operator ==(UInt4? one, UInt4? two)
    {
        if (!one.HasValue && !two.HasValue)
            return true;
        if (!one.HasValue)
            return false;
        if (!two.HasValue)
            return false;
        return one.Equals(two);
    }

    public override int GetHashCode() => (X, Y, Z, W).GetHashCode();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof(UInt4)) return false;
        return Equals((UInt4)obj);
    }

    public bool Equals(UInt4 other)
    {
        return
            X == other.X &&
            Y == other.Y &&
            Z == other.Z &&
            W == other.W;
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}, Z: {Z}, W: {W}";
    }
}
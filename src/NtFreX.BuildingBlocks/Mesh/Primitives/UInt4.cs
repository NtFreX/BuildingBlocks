using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct UInt4 : IEquatable<UInt4>
{
    public uint X, Y, Z, W;

    public static bool operator !=(UInt4? one, UInt4? two)
        => !(one == two);

    public static bool operator ==(UInt4? one, UInt4? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode() 
        => (X, Y, Z, W).GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(UInt4 other)
        => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    public override string ToString()
        => $"X: {X}, Y: {Y}, Z: {Z}, W: {W}";
}
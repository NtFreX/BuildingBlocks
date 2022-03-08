using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public struct Index16 : IIndex<Index16>, IEquatable<Index16>
{
    public static IndexFormat IndexFormat => IndexFormat.UInt16;

    public ushort Value { get; init; }

    public static bool operator !=(Index16? one, Index16? two)
        => !(one == two);

    public static bool operator ==(Index16? one, Index16? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public static implicit operator Index16(int value)
    {
        checked
        {
            return new Index16 { Value = (ushort)value };
        }
    }

    public static implicit operator Index16(uint value)
    {
        checked
        {
            return new Index16 { Value = (ushort)value };
        }
    }

    public static implicit operator Index16(ushort value)
        => new () { Value = value };

    public static implicit operator ushort(Index16 value)
        => value.Value;
    
    public static explicit operator Index16(Index32 value)
    {
        checked
        {
            return new Index16 { Value = (ushort)value.Value };
        }
    }

    public static Index16 Parse(Index32 value) => (Index16)value;
    public static Index16 Parse(Index16 value) => value;
    public static Index16 ParseShort(ushort value) => value;
    public static Index16 ParseInt(uint value) => value;

    public uint AsUInt()
        => Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value.ToString() + " ushort";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(Index16 other)
        => other.Value == Value;
}

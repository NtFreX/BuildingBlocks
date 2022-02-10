using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public struct Index32 : IIndex<Index32>, IEquatable<Index32>
{
    public static IndexFormat IndexFormat => IndexFormat.UInt32;

    public uint Value { get; init; }


    public static implicit operator Index32(long value)
    {
        checked
        {
            return new Index32 { Value = (uint)value };
        }
    }

    public static implicit operator Index32(int value)
    {
        checked
        {
            return new Index32 { Value = (uint)value };
        }
    }

    public static implicit operator Index32(ushort value)
        => new Index32 { Value = value };

    public static implicit operator Index32(uint value)
        => new Index32 { Value = value };

    public static implicit operator uint(Index32 value)
        => value.Value;

    public static explicit operator Index32(Index16 value)
        => new Index32 { Value = value.Value };

    public static Index32 Parse(Index32 value) => value;
    public static Index32 Parse(Index16 value) => (Index32) value;
    public static Index32 ParseShort(ushort value) => value;
    public static Index32 ParseInt(uint value) => value;

    public uint AsUInt()
        => Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value.ToString() + " uint";

    public bool Equals(Index32 other)
        => Value == other.Value;
}

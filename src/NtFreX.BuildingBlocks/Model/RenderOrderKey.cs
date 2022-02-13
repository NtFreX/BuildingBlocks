using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Model;

public struct RenderOrderKey : IComparable<RenderOrderKey>, IComparable
{
    public readonly ulong Value;

    public RenderOrderKey(ulong value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderOrderKey Create(int materialID, float cameraDistance, float camaraFarDistance)
        => Create((uint)materialID, cameraDistance, camaraFarDistance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderOrderKey Create(uint materialID, float cameraDistance, float camaraFarDistance)
    {
        uint cameraDistanceInt = (uint)Math.Min(uint.MaxValue, cameraDistance * camaraFarDistance);

        return new RenderOrderKey(
            ((ulong)materialID << 32) +
            cameraDistanceInt);
    }

    public int CompareTo(RenderOrderKey other)
        => Value.CompareTo(other.Value);

    public int CompareTo(object? obj)
        => Value.CompareTo(obj);
}
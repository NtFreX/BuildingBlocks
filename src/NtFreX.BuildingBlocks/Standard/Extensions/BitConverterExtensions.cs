using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class BitConverterExtensions
{
    public static unsafe byte[] ToBytes<T>(in T value) where T : unmanaged
    {
        byte[] result = new byte[Unsafe.SizeOf<T>()];
        Unsafe.As<byte, T>(ref result[0]) = value;
        return result;
    }

    public static T FromBytes<T>(byte[] data) where T : unmanaged
        => Unsafe.As<byte, T>(ref data[0]);

}

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class BitConverterExtensions
{
    public static unsafe byte[] SingleToBytes<T>(in T value) where T : unmanaged
    {
        byte[] result = new byte[Unsafe.SizeOf<T>()];
        Unsafe.As<byte, T>(ref result[0]) = value;
        return result;
    }

    public static T SingleFromBytes<T>(Span<byte> data) where T : unmanaged
        => Unsafe.As<byte, T>(ref data[0]);


    public unsafe static byte[] ArrayToBytes<T>(in T[] value) where T: unmanaged
    {
        var buffer = new byte[Unsafe.SizeOf<T>() * value.Length];
        fixed (T* ptr = value)
        {
            Marshal.Copy(new IntPtr(ptr), buffer, 0, buffer.Length);
        }
        return buffer;
    }

    public unsafe static T[] ArrayFromBytes<T>(in byte[] data) where T : unmanaged
    {
        var buffer = new T[data.Length /  Unsafe.SizeOf<T>()];
        fixed(T* ptrManaged = buffer)
        {
            Marshal.Copy(data, 0, new IntPtr(ptrManaged), data.Length);
        }
        return buffer;
    }
}

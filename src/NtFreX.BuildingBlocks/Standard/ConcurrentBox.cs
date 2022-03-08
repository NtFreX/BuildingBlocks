using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Standard;

internal sealed class ConcurrentBox<T>
{
    private readonly object _lock = new ();

    internal record Live(bool IsDead, T Value);

    private const int DataPartSize = 1024;
    private const int CleanupDeadCount = DataPartSize / 4;

    private Live[] data;
    private int index = 0;
    private int deads = 0;

    public ConcurrentBox()
    {
        data = new Live[DataPartSize];
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            if (index >= DataPartSize)
                Array.Resize(ref data, data.Length + DataPartSize);

            data[index++] = new Live(false, item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Live Get(int index)
        => data[index];

    public void Kill(int index)
    {
        lock (_lock)
        {
            data[index] = data[index] with { IsDead = true };
            deads++;
        }
    }

    public void Cleanup()
    {
        lock (_lock)
        {
            if (deads < CleanupDeadCount)
                return;

            var realIndex = 0;
            for (var i = 0; i < index; i++)
            {
                data[realIndex] = data[i];

                if (!data[i].IsDead)
                    realIndex++;
            }
            index = realIndex;

            deads = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count() => index;
}
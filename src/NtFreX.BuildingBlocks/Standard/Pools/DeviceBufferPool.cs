using System.Collections.Concurrent;
using Veldrid;

namespace NtFreX.BuildingBlocks.Standard.Pools
{
    public sealed class DeviceBufferPool : IDisposable
    {
        private readonly ConcurrentDictionary<(BufferUsage, uint), ConcurrentStack<PooledDeviceBuffer>> vertexBufferPool = new ();
        private readonly uint blockSize;

        public DeviceBufferPool(uint blockSize)
        {
            this.blockSize = blockSize;
        }

        private uint GetNextBlock(uint size)
            => size % blockSize == 0 ? size : size - size % blockSize + blockSize;

        private void OnPoolReleased(object? sender, EventArgs args)
        {
            var buffer = sender as PooledDeviceBuffer ?? throw new Exception();
            buffer.PoolReleased -= OnPoolReleased;
            if (vertexBufferPool.TryGetValue((buffer.Usage, GetNextBlock(buffer.SizeInBytes)), out var pools))
            {
                pools.Push(buffer);
            }
        }

        public PooledDeviceBuffer CreateBuffer(ResourceFactory factory, BufferUsage bufferUsage, uint sizeInBytes)
        {
            //var poolKey = GetPoolKey(bufferUsage, sizeInBytes);
            var nextBlock = GetNextBlock(sizeInBytes);
            var poolKey = (bufferUsage, nextBlock);
            if (!vertexBufferPool.TryGetValue(poolKey, out var pools))
            {
                pools = new ConcurrentStack<PooledDeviceBuffer>();
                vertexBufferPool.TryAdd(poolKey, pools);
            }

            if (pools.TryPop(out var freeBuffer))
            {
                freeBuffer.Use();
                freeBuffer.PoolReleased += OnPoolReleased;
                return freeBuffer;
            }

            var buffer = new PooledDeviceBuffer(factory.CreateBuffer(new BufferDescription(nextBlock, bufferUsage)), destroyDirectly: false);
            pools.Push(buffer);
            return buffer;
        }

        public void Dispose()
        {
            foreach(var pools in vertexBufferPool)
            {
                foreach (var buffer in pools.Value)
                {
                    buffer.Destroy();
                }
            }
        }
    }
}

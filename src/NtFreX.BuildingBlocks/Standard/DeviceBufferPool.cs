using System.Collections.Concurrent;
using Veldrid;

namespace NtFreX.BuildingBlocks.Standard
{
    public class DeviceBufferPool : IDisposable
    {
        private readonly ConcurrentDictionary<(BufferUsage, uint), ConcurrentStack<PooledDeviceBuffer>> vertexBufferPool = new ConcurrentDictionary<(BufferUsage, uint), ConcurrentStack<PooledDeviceBuffer>>();
        private readonly uint blockSize;

        public DeviceBufferPool(uint blockSize)
        {
            this.blockSize = blockSize;
        }

        private uint GetNextBlock(uint size)
            => size % blockSize == 0 ? size : size - size % blockSize + blockSize;
        //private string GetPoolKey(BufferUsage bufferUsage, uint sizeInBytes)
        //{
        //    var nextBlock = GetNextBlock(sizeInBytes).ToString();
        //    var usage = bufferUsage.ToString();
        //    return string.Create(nextBlock.Length + usage.Length, (nextBlock, usage), (buffer, state) => 
        //    {
        //        state.nextBlock.AsSpan().CopyTo(buffer);
        //        state.usage.AsSpan().CopyTo(buffer.Slice(nextBlock.Length));
        //    });
        //}

        private void OnPoolReleased(object? sender, EventArgs args)
        {
            var buffer = sender as PooledDeviceBuffer ?? throw new Exception();
            buffer.PoolReleased -= OnPoolReleased;

            //var key = GetPoolKey(buffer.Usage, buffer.SizeInBytes);
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

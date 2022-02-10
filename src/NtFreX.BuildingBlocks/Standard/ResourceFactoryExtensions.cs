using NtFreX.BuildingBlocks.Mesh;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Standard
{
    public static class ResourceFactoryExtensions
    {
        public static PooledDeviceBuffer CreatedPooledBuffer(this ResourceFactory resourceFactory, BufferDescription bufferDescription, DeviceBufferPool? deviceBufferPool = null)
        {
            return deviceBufferPool == null
                ? new PooledDeviceBuffer(resourceFactory.CreateBuffer(bufferDescription))
                : deviceBufferPool.CreateBuffer(resourceFactory, bufferDescription.Usage, bufferDescription.SizeInBytes);
        }

        private static PooledDeviceBuffer? emptyInstanceBuffer;
        private static InstanceInfo emptyInstance = InstanceInfo.Single.First();
        public static PooledDeviceBuffer GetInstanceBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, InstanceInfo[]? instances, DeviceBufferPool? deviceBufferPool = null)
        {
            if (instances == null || (instances.Length == 1 && instances[0].Equals(emptyInstance)))
                return GetEmptyInstanceBuffer(resourceFactory, graphicsDevice, deviceBufferPool);

            var instanceBufferDesc = new BufferDescription((uint)(InstanceInfo.Size * instances.Length), BufferUsage.VertexBuffer);
            var buffer = resourceFactory.CreatedPooledBuffer(instanceBufferDesc, deviceBufferPool);
            graphicsDevice.UpdateBuffer(buffer.RealDeviceBuffer, 0, instances);

            return buffer;
        }

        public static PooledDeviceBuffer GetEmptyInstanceBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, DeviceBufferPool? deviceBufferPool = null)
        {
            if (emptyInstanceBuffer == null)
            {
                var realInstances = InstanceInfo.Single;
                var instanceBufferDesc = new BufferDescription((uint)(InstanceInfo.Size * realInstances.Length), BufferUsage.VertexBuffer);
                emptyInstanceBuffer = resourceFactory.CreatedPooledBuffer(instanceBufferDesc, deviceBufferPool);
                graphicsDevice.UpdateBuffer(emptyInstanceBuffer.RealDeviceBuffer, 0, realInstances);
            }
            return emptyInstanceBuffer;

        }

        private static Dictionary<MaterialInfo, PooledDeviceBuffer> materialBufferCache = new Dictionary<MaterialInfo, PooledDeviceBuffer>();
        public static PooledDeviceBuffer GetMaterialBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, MaterialInfo material, DeviceBufferPool? deviceBufferPool = null)
        {
            if(materialBufferCache.TryGetValue(material, out var buffer))
            {
                return buffer;
            }

            var materialBufferDesc = new BufferDescription((uint)Marshal.SizeOf<MaterialInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic);
            var materialBuffer = resourceFactory.CreatedPooledBuffer(materialBufferDesc, deviceBufferPool);
            graphicsDevice.UpdateBuffer(materialBuffer.RealDeviceBuffer, 0, material);
            materialBufferCache.Add(material, materialBuffer);
            return materialBuffer;
        }

    }
}

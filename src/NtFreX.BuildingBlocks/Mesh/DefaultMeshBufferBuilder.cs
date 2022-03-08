using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Standard.Pools;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public class DefaultMeshBufferBuilder : MeshBufferBuilder
{
    public override (PooledDeviceBuffer VertexBuffer, PooledDeviceBuffer IndexBuffer, uint IndexCount) Build(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, SpecializedMeshData meshDataProvider, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        => meshDataProvider.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory, deviceBufferPool, commandListPool);
}

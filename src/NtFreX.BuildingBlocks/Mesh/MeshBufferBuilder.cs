using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Standard.Pools;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public abstract class MeshBufferBuilder
{
    public abstract (PooledDeviceBuffer VertexBuffer, PooledDeviceBuffer IndexBuffer, uint IndexCount) Build(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, SpecializedMeshData meshDataProvider, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null);
}

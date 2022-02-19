using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class TextureModel
{
    public static MeshRenderer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, TextureView texture, Transform? transform = null, DeviceBufferPool? deviceBufferPool = null)
    {
        return PlaneModel.Create(
            graphicsDevice, resourceFactory, graphicsSystem,
            transform: transform,
            texture: texture,
            deviceBufferPool: deviceBufferPool
        );
    }
}


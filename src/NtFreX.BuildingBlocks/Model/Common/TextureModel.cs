using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model.Common;

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


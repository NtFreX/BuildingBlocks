using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;

namespace NtFreX.BuildingBlocks.Models
{
    public static class TextureModel
    {
        public static Model Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders, TextureView texture, ModelCreationInfo? creationInfo = null, DeviceBufferPool? deviceBufferPool = null)
        {
            return PlaneModel.Create(
                graphicsDevice, resourceFactory, graphicsSystem,
                shaders,
                creationInfo: creationInfo,
                texture: texture,
                deviceBufferPool: deviceBufferPool
            );
        }
    }
}

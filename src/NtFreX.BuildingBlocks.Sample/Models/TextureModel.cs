using NtFreX.BuildingBlocks.Models;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    public static class TextureModel
    {
        public static Model Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, ModelCreationInfo creationInfo, Shader[] shaders, TextureView texture)
        {
            return PlaneModel.Create(
                graphicsDevice, resourceFactory, graphicsSystem,
                creationInfo,
                shaders, 
                texture: texture
            );
        }
    }
}

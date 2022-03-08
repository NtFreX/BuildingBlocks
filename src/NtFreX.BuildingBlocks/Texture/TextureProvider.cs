using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

public abstract class TextureProvider
{
    public abstract Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory);
}

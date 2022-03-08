using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

// TODO: is this really needed?
public class DynamicTexureProvider : TextureProvider
{
    private readonly Func<GraphicsDevice, ResourceFactory, Task<TextureView>> textureProvider;

    public DynamicTexureProvider(Func<GraphicsDevice, ResourceFactory, Task<TextureView>> textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    public override Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        => textureProvider(graphicsDevice, resourceFactory);

    public override int GetHashCode()
        => textureProvider.GetHashCode();
}

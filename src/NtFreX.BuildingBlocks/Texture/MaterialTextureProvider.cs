using NtFreX.BuildingBlocks.Material;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

public class MaterialTextureProvider : TextureProvider
{
    private readonly string identifier;

    public MaterialTextureProvider(string identifier)
    {
        this.identifier = identifier;
    }

    public override Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        => Task.FromResult(MaterialTextureFactory.Instance.GetOutput(identifier));
}

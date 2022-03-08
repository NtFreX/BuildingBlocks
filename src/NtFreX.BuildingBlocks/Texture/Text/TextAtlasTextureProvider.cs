using NtFreX.BuildingBlocks.Texture.Text;
using SixLabors.Fonts;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

public class TextAtlasTextureProvider : TextureProvider
{
    private readonly Font font;
    private readonly bool alpha;

    public TextAtlasTextureProvider(Font font, bool alpha)
    {
        this.font = font;
        this.alpha = alpha;
    }

    public override Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        var atlas = TextAtlas.ForFont(font);
        atlas.CreateDeviceResources(graphicsDevice, resourceFactory);

        if (alpha)
        {
            Debug.Assert(atlas.AlphaTexture != null);
            return Task.FromResult(atlas.AlphaTexture);
        }
        else
        {
            Debug.Assert(atlas.Texture != null);
            return Task.FromResult(atlas.Texture);
        }
    }
}

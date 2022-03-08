using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

//TODO: delete this (this class leaves the responsibility to create the texture view for new graphic devices at the user)
public class StaticTextureProvider : TextureProvider
{
    public readonly TextureView TextureView;

    public StaticTextureProvider(TextureView textureView)
    {
        TextureView = textureView;
    }

    public static bool operator !=(StaticTextureProvider? one, StaticTextureProvider? two)
        => !(one == two);

    public static bool operator ==(StaticTextureProvider? one, StaticTextureProvider? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(StaticTextureProvider? other)
        => other?.TextureView == TextureView;

    public override int GetHashCode()
        => TextureView.GetHashCode();

    public override string ToString()
        => $"TextureView: {TextureView}";

    public override Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        => Task.FromResult(TextureView);
}

using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

public class DirectoryTextureProvider : TextureProvider
{
    private readonly TextureFactory textureFactory;

    public readonly string TexturePath;
    public readonly bool Srgb;
    public readonly bool Mipmap;
    
    public DirectoryTextureProvider(TextureFactory textureFactory, string texturePath, bool mipmap = true, bool srgb = false)
    {
        this.textureFactory = textureFactory;
        TexturePath = texturePath;
        this.Srgb = srgb;
        this.Mipmap = mipmap;
    }

    public static bool operator !=(DirectoryTextureProvider? one, DirectoryTextureProvider? two)
        => !(one == two);

    public static bool operator ==(DirectoryTextureProvider? one, DirectoryTextureProvider? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(DirectoryTextureProvider? other)
        => other?.TexturePath == TexturePath && other?.Mipmap == Mipmap && other?.Srgb == Srgb;

    public override int GetHashCode()
        => (Mipmap, Srgb, TexturePath).GetHashCode();

    public override string ToString()
        => $"TexturePath: {TexturePath}, Mipmap: {Mipmap}, Srgb: {Srgb}";

    public override Task<TextureView> GetAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        => textureFactory.GetTextureAsync(graphicsDevice, resourceFactory, TexturePath, Mipmap, Srgb);
}
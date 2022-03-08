using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.ImageSharp;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Texture;

//public class ImageSharpTexture3D
//{
//    public Image<Rgba32>[] Images { get; }
//    public uint Width { get; }
//    public uint Height { get; }
//    public PixelFormat Format { get; }
//    public uint PixelSizeInBytes => sizeof(byte) * 4;

//    public ImageSharpTexture3D(Image<Rgba32>[] images, uint width, uint height, bool srgb = false)
//    {
//        Format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
//        Width = width;
//        Height = height;
//    }

//    public unsafe VeldridTexture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory)
//        => CreateTextureViaUpdate(gd, factory);

//    private unsafe VeldridTexture CreateTextureViaUpdate(GraphicsDevice gd, ResourceFactory factory)
//    {
//        var tex = factory.CreateTexture(TextureDescription.Texture3D(
//            Width, Height, (uint)Images.Length, 1, Format, TextureUsage.Sampled));
//        for (int level = 0; level < Images.Length; level++)
//        {
//            Image<Rgba32> image = Images[level];
//            if (!image.TryGetSinglePixelSpan(out Span<Rgba32> pixelSpan))
//            {
//                throw new VeldridException("Unable to get image pixelspan.");
//            }
//            fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
//            {
//                gd.UpdateTexture(
//                    tex,
//                    (IntPtr)pin,
//                    (uint)(PixelSizeInBytes * image.Width * image.Height),
//                    0,
//                    0,
//                    0,
//                    (uint)image.Width,
//                    (uint)image.Height,
//                    1,
//                    (uint)level,
//                    0);
//            }
//        }
//        return tex;
//    }
//}

public class TextureFactory
{
    private readonly ILogger<TextureFactory> logger;
    private readonly ConcurrentDictionary<string, TextureView> textures = new ();
    private TextureView? defaultTextureView;
    private TextureView? emptyTextureView;

    private ImageSharpTexture defaultTexture;
    private ImageSharpTexture emptyTexture;

    public TextureFactory(ILogger<TextureFactory> logger)
    {
        this.logger = logger;

        defaultTexture = new ImageSharpTexture(TextureCreator.CreateMissingTexture(SystemFonts.Find("Arial")), mipmap: true);
        emptyTexture = new ImageSharpTexture(TextureCreator.CreateEmptyTexture(), mipmap: true);
    }

    private TextureView LoadTexture(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Image<Rgba32> image, bool mipmap, bool srgb)
    {
        var processedTexture = new ImageSharpTexture(image, mipmap, srgb);
        //TODO: this can't be disposed here or vulkan memory will corrupt
        var surfaceTexture = processedTexture.CreateDeviceTexture(graphicsDevice, resourceFactory);
        return resourceFactory.CreateTextureView(surfaceTexture);
    }

    private async Task<TextureView> LoadTextureAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, string fullPath, bool mipmap, bool srgb)
    {
        if (textures.TryGetValue(fullPath, out var texture))
            return texture;

        var image = await Image.LoadAsync<Rgba32>(fullPath);
        var surfaceTextureView = LoadTexture(graphicsDevice, resourceFactory, image, mipmap, srgb);

        textures.TryAdd(fullPath, surfaceTextureView);

        return surfaceTextureView;
    }

    public async Task SetDefaultTextureAsync(string path, bool mipmap = true, bool srgb = false)
    {
        defaultTexture = new ImageSharpTexture(await Image.LoadAsync<Rgba32>(path), mipmap, srgb); 
        defaultTextureView?.Dispose();
        defaultTextureView = null;
    }

    public async Task SetEmptyTextureAsync(string path, bool mipmap = true, bool srgb = false)
    {
        emptyTexture = new ImageSharpTexture(await Image.LoadAsync<Rgba32>(path), mipmap, srgb);
        emptyTextureView?.Dispose();
        emptyTextureView = null;
    }

    public TextureView GetDefaultTexture(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if(defaultTextureView == null)
        {
            //TODO: this can't be disposed here or vulkan memory will corrupt
            var texture = defaultTexture.CreateDeviceTexture(graphicsDevice, resourceFactory);
            return resourceFactory.CreateTextureView(texture); //TODO: texture view type for array support?
        }
        return defaultTextureView;
    }

    public TextureView GetEmptyTexture(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if (emptyTextureView == null)
        {
            //TODO: this can't be disposed here or vulkan memory will corrupt
            var texture = emptyTexture.CreateDeviceTexture(graphicsDevice, resourceFactory);
            return resourceFactory.CreateTextureView(texture); //TODO: texture view type for array support?
        }
        return emptyTextureView;
    }

    public async Task<TextureView> GetTextureAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, string? fullPath, bool mipmap = true, bool srgb = false)
    {
        if (File.Exists(fullPath))
        {
            return await LoadTextureAsync(graphicsDevice, resourceFactory, fullPath, mipmap, srgb);
        }

        logger.LogWarning($"The texture with the path {fullPath} has not been found");
        logger.LogInformation($"The default texture will be used");

        return GetDefaultTexture(graphicsDevice, resourceFactory);
    }

    public void DestroyDeviceResources()
    {
        defaultTextureView?.Dispose();
        emptyTextureView?.Dispose();
        foreach (var texture in textures.Values)
        {
            texture.Dispose();
        }
        textures.Clear();
    }
}
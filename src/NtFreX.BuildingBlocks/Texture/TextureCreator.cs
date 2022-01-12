using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture
{
    public static class TextureCreator
    {
        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, string text, Color color, PointF location, FontFamily fontFamily, float fontSize, FontStyle fontStyle = FontStyle.Regular, Action<IImageProcessingContext>? mutateImage = null)
            => Create(graphicsDevice, resourceFactory, text, color, location, fontFamily.CreateFont(fontSize, fontStyle), mutateImage);
        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, string text, Color color, PointF location, Font font, Action<IImageProcessingContext>? mutateImage = null)
        {
            var size = TextMeasurer.Measure(text, new RendererOptions(font));
            return Create(graphicsDevice, resourceFactory, (int)size.Width, (int)size.Height, img =>
            {
                mutateImage?.Invoke(img);
                img.DrawText(text, font, color, location);
            });
        }
        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, int width, int height, Action<IImageProcessingContext> mutateImage)
        {
            using (Image<Rgba32> img = new Image<Rgba32>(width, height))
            {
                img.Mutate(mutateImage);
                return Create(graphicsDevice, resourceFactory, img);
            }
        }
        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Image<Rgba32> image)
        {
            var texture = ProcessedTexture.Read(image);
            var surfaceTexture = texture.CreateDeviceTexture(graphicsDevice, resourceFactory, TextureUsage.Sampled);
            return resourceFactory.CreateTextureView(surfaceTexture);
        }
    }
}

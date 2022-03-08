using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;
using Veldrid.ImageSharp;

namespace NtFreX.BuildingBlocks.Texture
{
    public static class TextureCreator
    {
        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, int width, int height, Action<IImageProcessingContext> mutateImage, bool mipmap = true, bool srgb = false)
        {
            //TODO: dispose (here possible?)
            var img = new Image<Rgba32>(width, height);
            img.Mutate(mutateImage);
            return Create(graphicsDevice, resourceFactory, img, mipmap, srgb);
        }

        public static TextureView Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Image<Rgba32> image, bool mipmap = true, bool srgb = false)
        {
            var texture = new ImageSharpTexture(image, mipmap, srgb);
            //TODO: sispose somewhere (not here because of vulcan)
            var surfaceTexture = texture.CreateDeviceTexture(graphicsDevice, resourceFactory);
            return resourceFactory.CreateTextureView(surfaceTexture);
        }

        public static Image<Rgba32> CreateEmptyTexture(int size = 1)
            => new (size, size, new Rgba32(0, 0, 0, 1));

        public static Image<Rgba32> CreateMissingTexture(FontFamily fontFamily, int size = 500)
        {
            //TODO: dispose (here possible?)
            var img = new Image<Rgba32>(size, size);
            img.Mutate((context) => 
            {
                context.BackgroundColor(new Color(new Rgba32(255, 165, 0)));
                context.DrawLines(new Pen(Color.Black, 1f), new PointF(0, 0), new PointF(size - 1, 0), new PointF(size - 1, size - 1), new PointF(0, size - 1), new PointF(0, 0));

                for (var i = 10; i < size; i += 10)
                {
                    if (i % 100 == 0)
                    {
                        var length = size / 2 / 8;
                        context.DrawLines(new Pen(Color.Black, 4f), new PointF(i - 1, 0), new PointF(i - 1, length));
                        context.DrawLines(new Pen(Color.Black, 4f), new PointF(i - 1, size - 1), new PointF(i - 1, size - length - 1));
                        context.DrawLines(new Pen(Color.Black, 4f), new PointF(0, i - 1), new PointF(length, i - 1));
                        context.DrawLines(new Pen(Color.Black, 4f), new PointF(size - 1, i - 1), new PointF(size - length - 1, i - 1));

                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(i - 1, 0), new PointF(i - 1, size));
                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(0, i - 1), new PointF(size, i - 1));

                        var fontSize = 22;
                        var font = fontFamily.CreateFont(fontSize);
                        var text = i.ToString();
                        var measure = TextMeasurer.Measure(text, new RendererOptions(font));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(i - 1 - measure.Width / 2, length));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(i - 1 - measure.Width / 2, size - 1 - length - measure.Height));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(length, i - 1 - measure.Height / 2));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(size - 1 - length - measure.Width, i - 1 - measure.Height / 2));
                    }
                    else if (i % 50 == 0)
                    {
                        var length = size / 2 / 10;
                        context.DrawLines(new Pen(Color.Black, 2f), new PointF(i - 1, 0), new PointF(i - 1, length));
                        context.DrawLines(new Pen(Color.Black, 2f), new PointF(i - 1, size - 1), new PointF(i - 1, size - length - 1));
                        context.DrawLines(new Pen(Color.Black, 2f), new PointF(0, i - 1), new PointF(length, i - 1));
                        context.DrawLines(new Pen(Color.Black, 2f), new PointF(size - 1, i - 1), new PointF(size - length - 1, i - 1));

                        var fontSize = 12;
                        var font = fontFamily.CreateFont(fontSize);
                        var text = i.ToString();
                        var measure = TextMeasurer.Measure(text, new RendererOptions(font));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(i - 1 - measure.Width / 2, length));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(i - 1 - measure.Width / 2, size - 1 - length - measure.Height));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(length, i - 1 - measure.Height / 2));
                        context.DrawText(i.ToString(), font, Color.Black, new PointF(size - 1 - length - measure.Width, i - 1 - measure.Height / 2));
                    }
                    else
                    {
                        var length = size / 2 / 20;
                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(i - 1, 0), new PointF(i - 1, length));
                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(i - 1, size - 1), new PointF(i - 1, size - length - 1));
                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(0, i - 1), new PointF(length, i - 1));
                        context.DrawLines(new Pen(Color.Black, 1f), new PointF(size - 1, i - 1), new PointF(size - length - 1, i - 1));
                    }
                }
            });
            return img;
        }
    }
}

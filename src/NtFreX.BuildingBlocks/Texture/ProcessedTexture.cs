using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture
{
    public class ProcessedTexture
    {
        public PixelFormat Format { get; set; }
        public TextureType Type { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Depth { get; set; }
        public uint MipLevels { get; set; }
        public uint ArrayLayers { get; set; }
        public byte[] TextureData { get; set; }

        public ProcessedTexture(
            PixelFormat format,
            TextureType type,
            uint width,
            uint height,
            uint depth,
            uint mipLevels,
            uint arrayLayers,
            byte[] textureData)
        {
            Format = format;
            Type = type;
            Width = width;
            Height = height;
            Depth = depth;
            MipLevels = mipLevels;
            ArrayLayers = arrayLayers;
            TextureData = textureData;
        }

        public static async Task<ProcessedTexture> ReadAsync(string name)
        {
            using (var stream = File.OpenRead(name))
            {
                var image = await Image.LoadAsync<Rgba32>(stream).ConfigureAwait(false);
                return Read(image);
            }
        }

        public static unsafe ProcessedTexture Read(Image<Rgba32> image)
        {
            Image<Rgba32>[] mipmaps = GenerateMipmaps(image, out int totalSize);

            byte[] allTexData = new byte[totalSize];
            long offset = 0;
            fixed (byte* allTexDataPtr = allTexData)
            {
                foreach (Image<Rgba32> mipmap in mipmaps)
                {
                    long mipSize = mipmap.Width * mipmap.Height * sizeof(Rgba32);
                    if (!mipmap.TryGetSinglePixelSpan(out Span<Rgba32> pixelSpan))
                    {
                        throw new VeldridException("Unable to get image pixelspan.");
                    }
                    fixed (void* pixelPtr = &MemoryMarshal.GetReference(pixelSpan))
                    {
                        Buffer.MemoryCopy(pixelPtr, allTexDataPtr + offset, mipSize, mipSize);
                    }

                    offset += mipSize;
                }
            }

            return new ProcessedTexture(
                PixelFormat.R8_G8_B8_A8_UNorm, TextureType.Texture2D,
                (uint)image.Width, (uint)image.Height, 1,
                (uint)mipmaps.Length, 1,
                allTexData);
        }

        private static Image<T>[] GenerateMipmaps<T>(Image<T> baseImage, out int totalSize) where T : unmanaged, IPixel<T>
        {
            int mipLevelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
            Image<T>[] mipLevels = new Image<T>[mipLevelCount];
            mipLevels[0] = baseImage;
            totalSize = baseImage.Width * baseImage.Height * Marshal.SizeOf<T>();
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while (currentWidth != 1 || currentHeight != 1)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<T> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                totalSize += newWidth * newHeight * Marshal.SizeOf<T>();
                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }

        private static int ComputeMipLevels(int width, int height)
        {
            return 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }

        public unsafe Veldrid.Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory rf, TextureUsage usage)
        {
            Veldrid.Texture texture = rf.CreateTexture(new TextureDescription(
                Width, Height, Depth, MipLevels, ArrayLayers, Format, usage, Type));

            Veldrid.Texture staging = rf.CreateTexture(new TextureDescription(
                Width, Height, Depth, MipLevels, ArrayLayers, Format, TextureUsage.Staging, Type));

            ulong offset = 0;
            fixed (byte* texDataPtr = &TextureData[0])
            {
                for (uint level = 0; level < MipLevels; level++)
                {
                    uint mipWidth = GetDimension(Width, level);
                    uint mipHeight = GetDimension(Height, level);
                    uint mipDepth = GetDimension(Depth, level);
                    uint subresourceSize = mipWidth * mipHeight * mipDepth * GetFormatSize(Format);

                    for (uint layer = 0; layer < ArrayLayers; layer++)
                    {
                        gd.UpdateTexture(
                            staging, (IntPtr)(texDataPtr + offset), subresourceSize,
                            0, 0, 0, mipWidth, mipHeight, mipDepth,
                            level, layer);
                        offset += subresourceSize;
                    }
                }
            }

            CommandList cl = rf.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(staging, texture);
            cl.End();
            gd.SubmitCommands(cl);
            gd.DisposeWhenIdle(cl);
            gd.DisposeWhenIdle(staging);

            return texture;
        }

        private uint GetFormatSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8_G8_B8_A8_UNorm: return 4;
                case PixelFormat.BC3_UNorm: return 1;
                default: throw new NotImplementedException();
            }
        }

        public static uint GetDimension(uint largestLevelDimension, uint mipLevel)
        {
            uint ret = largestLevelDimension;
            for (uint i = 0; i < mipLevel; i++)
            {
                ret /= 2;
            }

            return Math.Max(1, ret);
        }
    }
}

using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Texture.Text;

public class TextBuffer
{
    private class TextBufferPart
    {
        public readonly Font Font;
        public readonly string Text;
        public readonly List<VertexPositionNormalTextureColor> Vertices = new List<VertexPositionNormalTextureColor>();

        public TextBufferPart(Font font, string text)
        {
            Font = font;
            Text = text;
        }
    }

    private static uint MaxTextLengthSizeIncrease = 1000;
    private static uint MaxTextLength = 5000;
    private static PooledDeviceBuffer? IndexBuffer;

    private readonly List<TextBufferPart> textBufferParts = new List<TextBufferPart>();
    private readonly char newLineChar = '\r';
    private readonly char[] charactersToIgnore = new[] { '\n' };

    private PointF cursorPosition = new PointF(0, 0);
    private BoundingBox boundingBox = new BoundingBox(); //TODO: seems to be incorrect (very very slightly)?

    public void Append(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Font font, string text, RgbaFloat color)
    {
        var atlas = TextAtlas.ForFont(graphicsDevice, resourceFactory, font);
        try
        {
            var part = new TextBufferPart(font, text);
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == newLineChar)
                {
                    cursorPosition = new PointF(0, cursorPosition.Y - atlas.LineHeight);
                }
                else if (!charactersToIgnore.Contains(text[i]))
                {
                    var position = atlas.Characters[text[i]];

                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(cursorPosition.X, cursorPosition.Y + atlas.LineHeight, 0), color, new Vector2(position.X / atlas.Size.Width, 0)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(cursorPosition.X, cursorPosition.Y, 0), color, new Vector2(position.X / atlas.Size.Width, position.Height / atlas.Size.Height)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(cursorPosition.X + position.Width, cursorPosition.Y + atlas.LineHeight, 0), color, new Vector2((position.X + position.Width) / atlas.Size.Width, 0)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(cursorPosition.X + position.Width, cursorPosition.Y, 0), color, new Vector2((position.X + position.Width) / atlas.Size.Width, position.Height / atlas.Size.Height)));

                    cursorPosition += new PointF(position.Width, 0);
                }
            }

            boundingBox = new BoundingBox(Vector3.Zero, new Vector3(cursorPosition.X, cursorPosition.Y  + atlas.LineHeight, 0));
            textBufferParts.Add(part);
        }
        catch
        {
            // try to use the atlas first and only reload it when it misses glyphs
            // this should be the exception case and removing the check if the glyph exists saves performance
            atlas.Load(graphicsDevice, resourceFactory, text);
            Append(graphicsDevice, resourceFactory, font, text, color);
        }
    }

    public MeshDeviceBuffer[] Build(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, out Span<MeshDeviceBuffer> data, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    {
        var buffers = ArrayPool<MeshDeviceBuffer>.Shared.Rent(textBufferParts.Count);
        var index = 0;
        foreach(var part in textBufferParts)
        {
            // TODO: do not provide normal?
            var mesh = new MeshDataProvider<VertexPositionNormalTextureColor, Index16>(part.Vertices.ToArray(), Array.Empty<Index16>(), PrimitiveTopology.TriangleList);

            var commandList = CommandListPool.TryGet(resourceFactory, commandListPool);
            var vertexBuffer = mesh.CreateVertexBuffer(resourceFactory, commandList.Item, deviceBufferPool);

            var textLength = part.Vertices.Count / 4;
            BuildIndexBuffer(resourceFactory, commandList, textLength, deviceBufferPool);

            CommandListPool.TryClean(graphicsDevice, commandList, commandListPool);

            // TODO: better buffer factories
            var materialBuffer = resourceFactory.GetMaterialBuffer(graphicsDevice, mesh.Material, deviceBufferPool);
            var instanceBuffer = resourceFactory.GetInstanceBuffer(graphicsDevice, InstanceInfo.Single, deviceBufferPool);

            var texture = TextAtlas.ForFont(graphicsDevice, resourceFactory, part.Font).Texture;
            buffers[index++] = new MeshDeviceBuffer(vertexBuffer, IndexBuffer!, (uint)textLength * 6, boundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, materialBuffer, mesh.Material, instanceInfoBuffer: instanceBuffer, instances: InstanceInfo.Single, textureView: texture);
        }
        data = buffers.AsSpan(0, textBufferParts.Count);
        return buffers;
    }

    public void Shutdown()
    {
        IndexBuffer?.Destroy();
    }

    private void BuildIndexBuffer(ResourceFactory resourceFactory, PooledCommandList commandList, int textLength, DeviceBufferPool? deviceBufferPool = null)
    {
        // TODO: create new model part instead of resizing buffer?
        if (IndexBuffer != null && MaxTextLength < textLength)
        {
            IndexBuffer.Destroy();
            IndexBuffer = null;
            while (MaxTextLength < textLength)
            {
                MaxTextLength += MaxTextLengthSizeIncrease;
                // we need 6 indices per character, the index format is 16 bits (ushort)
                if (MaxTextLength * 6 > ushort.MaxValue)
                    throw new Exception($"Only texts with a max length of {ushort.MaxValue / 6} are supported");
            }
        }
        if (IndexBuffer == null)
        {
            var desc = new BufferDescription((uint)(MaxTextLength * 6 * Marshal.SizeOf(typeof(ushort))), BufferUsage.IndexBuffer);
            IndexBuffer = resourceFactory.CreatedPooledBuffer(desc, deviceBufferPool);
            commandList.Item.UpdateBuffer(IndexBuffer.RealDeviceBuffer, 0, BuildIndices());
        }
    }

    private ushort[] BuildIndices()
    {
        var indices = new ushort[MaxTextLength * 6];
        for (var i = 0; i < MaxTextLength; i++)
        {
            indices[i * 6] = (ushort)(i * 4);
            indices[i * 6 + 1] = (ushort)(i * 4 + 2);
            indices[i * 6 + 2] = (ushort)(i * 4 + 1);

            indices[i * 6 + 3] = (ushort)(i * 4 + 2);
            indices[i * 6 + 4] = (ushort)(i * 4 + 1);
            indices[i * 6 + 5] = (ushort)(i * 4 + 3);
        }
        return indices;
    }
}

using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Texture.Text;

internal class TextRendererBuilder
{
    internal class TextBufferBuilder : MeshBufferBuilder
    {
        public override (PooledDeviceBuffer VertexBuffer, PooledDeviceBuffer IndexBuffer, uint IndexCount) Build(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, SpecializedMeshData meshDataProvider, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        {
            var commandList = CommandListPool.TryGet(resourceFactory, commandListPool: commandListPool);
            var vertexBuffer = meshDataProvider.CreateVertexBuffer(resourceFactory, commandList.CommandList, deviceBufferPool);

            var textLength = meshDataProvider.GetVertexCount() / 4;
            TextIndexBuffer.Instance.BuildIndexBuffer(resourceFactory, commandList.CommandList, textLength, deviceBufferPool);

            CommandListPool.TrySubmit(graphicsDevice, commandList, commandListPool);

            Debug.Assert(TextIndexBuffer.Instance.IndexBuffer != null);

            return (vertexBuffer, TextIndexBuffer.Instance.IndexBuffer, (uint)textLength * 6);
        }
    }

    internal class TextMeshDataProvider : MeshDataProvider
    {
        private readonly TextBufferPart textBufferPart;
        private readonly BoundingBox boundingBox;
        private readonly FaceCullMode faceCullMode;

        public TextMeshDataProvider(TextBufferPart textBufferPart, BoundingBox boundingBox, FaceCullMode faceCullMode = FaceCullMode.Front)
        {
            this.textBufferPart = textBufferPart;
            this.boundingBox = boundingBox;
            this.faceCullMode = faceCullMode;
        }

        public override Task<(SpecializedMeshData, BoundingBox)> GetAsync()
        {
            // TODO: better vertex type with less data
            var mesh = new DefinedMeshData<VertexPositionNormalTextureColor, Index16>(textBufferPart.Vertices.ToArray(), Array.Empty<Index16>(), PrimitiveTopology.TriangleList, faceCullMode: faceCullMode);

            var texture = new TextAtlasTextureProvider(textBufferPart.Font, alpha: false);
            var alphaTexture = new TextAtlasTextureProvider(textBufferPart.Font, alpha: true);
            mesh.Specializations.AddOrUpdate(new SurfaceTextureMeshDataSpecialization(texture));
            mesh.Specializations.AddOrUpdate(new AlphaMapMeshDataSpecialization(alphaTexture));

            return Task.FromResult((mesh as SpecializedMeshData, boundingBox));
        }
    }

    internal class TextBufferPart
    {
        public readonly Font Font;
        public readonly string Text;
        public readonly List<VertexPositionNormalTextureColor> Vertices = new ();

        public TextBufferPart(Font font, string text)
        {
            Font = font;
            Text = text;
        }
    }

    private readonly List<(TextBufferPart TextBufferPart, BoundingBox BoundingBox)> meshRenderVertices = new ();
    private readonly List<TextData> textData = new ();
    private readonly char newLineChar = '\r';
    private readonly char[] charactersToIgnore = new[] { '\n' };
    private readonly FaceCullMode faceCullMode;

    public PointF CursorPosition { get; private set; } = new PointF(0, 0);

    public TextData[] CurrentText => textData.ToArray();

    public TextRendererBuilder(FaceCullMode faceCullMode)
    {
        this.faceCullMode = faceCullMode;
    }

    public void Append(TextData text)
    {
        textData.Add(text);
        meshRenderVertices.Add(BuildTextPart(text));
    }

    public TextMeshDataProvider[] Build(int startFrom)
    {
        var buffers = new TextMeshDataProvider[textData.Count - startFrom];
        for (var index = startFrom; index < meshRenderVertices.Count; index++)
        {
            var part = meshRenderVertices[index];
            buffers[index] = new TextMeshDataProvider(part.TextBufferPart, part.BoundingBox, faceCullMode);
        }
        return buffers;
    }

    private (TextBufferPart TextBufferPart, BoundingBox BoundingBox) BuildTextPart(TextData text)
    {
        var atlas = TextAtlas.ForFont(text.Font);
        try
        {
            var part = new TextBufferPart(text.Font, text.Value);
            for (var i = 0; i < text.Value.Length; i++)
            {
                if (text.Value[i] == newLineChar)
                {
                    CursorPosition = new PointF(0, CursorPosition.Y - atlas.LineHeight);
                }
                else if (!charactersToIgnore.Contains(text.Value[i]))
                {
                    var position = atlas.Characters[text.Value[i]];

                    //TODO: normals
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(CursorPosition.X, CursorPosition.Y + atlas.LineHeight, 0), text.Color, new Vector2(position.X / atlas.Size.Width, 0)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(CursorPosition.X, CursorPosition.Y, 0), text.Color, new Vector2(position.X / atlas.Size.Width, position.Height / atlas.Size.Height)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(CursorPosition.X + position.Width, CursorPosition.Y + atlas.LineHeight, 0), text.Color, new Vector2((position.X + position.Width) / atlas.Size.Width, 0)));
                    part.Vertices.Add(new VertexPositionNormalTextureColor(new Vector3(CursorPosition.X + position.Width, CursorPosition.Y, 0), text.Color, new Vector2((position.X + position.Width) / atlas.Size.Width, position.Height / atlas.Size.Height)));

                    CursorPosition += new PointF(position.Width, 0);
                }
            }

            //TODO: seems to be incorrect (very very slightly)?
            var boundingBox = new BoundingBox(Vector3.Zero, new Vector3(CursorPosition.X, CursorPosition.Y + atlas.LineHeight, 0));
            return (part, boundingBox);
        }
        catch
        {
            // try to use the atlas first and only reload it when it misses glyphs
            // this should be the exception case and removing the check if the glyph exists saves performance
            atlas.Load(text.Value);
            return BuildTextPart(text);
        }
    }
}

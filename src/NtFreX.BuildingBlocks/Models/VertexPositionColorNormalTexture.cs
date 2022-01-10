using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public struct VertexPositionColorNormalTexture
    {
        public static VertexLayoutDescription VertexLayout =>
            new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("TextureCoordinates", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        public static ushort BytesBeforePosition => 0;

        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector2 TextureCoordinates;
        public readonly RgbaFloat Color;

        public VertexPositionColorNormalTexture(VertexPositionNormalTexture vertex) 
            : this(vertex.Position, new RgbaFloat(0, 0, 0, 0), vertex.TextureCoordinates, vertex.Normal) { }
        public VertexPositionColorNormalTexture(Vector3 position, RgbaFloat color)
            : this(position, color, Vector2.Zero) { }
        public VertexPositionColorNormalTexture(Vector3 position, RgbaFloat color, Vector2 textCoords)
            : this(position, color, textCoords, Vector3.Normalize(position)) { }
        public VertexPositionColorNormalTexture(Vector3 position, RgbaFloat color, Vector2 textCoords, Vector3 normal)
        {
            Position = position;
            Color = color;
            TextureCoordinates = textCoords;
            Normal = normal;
        }
    }
}

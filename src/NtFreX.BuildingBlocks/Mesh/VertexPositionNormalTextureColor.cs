using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh;

public struct VertexPositionNormalTextureColor : IVertex, IEquatable<VertexPositionNormalTextureColor>
{
    //TODO: set correct element semantic!
    public static VertexLayoutDescription VertexLayout { get; } =
        new VertexLayoutDescription(
            new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription(VertexElementSemantic.Normal.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription(VertexElementSemantic.TextureCoordinate.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription(VertexElementSemantic.Color.ToString(), VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

    public static ushort BytesBeforePosition => 0;
    public static RgbaFloat DefaultColor = new RgbaFloat(0, 0, 0, 1);

    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Vector2 TextureCoordinate;
    public readonly RgbaFloat Color;

    public unsafe static VertexPositionNormalTextureColor Build(byte[] data, VertexLayoutDescription layout)
    {
        var position = Vector3.Zero;
        var normal = Vector3.Zero;
        var textureCoordinate = Vector2.Zero;
        var color = DefaultColor;
        foreach(var element in layout.Elements)
        {
            if(element.Semantic == VertexElementSemantic.Position)
            {
                if (element.Format != VertexElementFormat.Float3)
                    throw new NotSupportedException("Only float3 are supported for the position");

                position = BitConverterExtensions.FromBytes<Vector3>(data);
            }

            if (element.Semantic == VertexElementSemantic.Normal)
            {
                if (element.Format != VertexElementFormat.Float3)
                    throw new NotSupportedException("Only float3 are supported for the normal");

                normal = BitConverterExtensions.FromBytes<Vector3>(data);
            }

            if (element.Semantic == VertexElementSemantic.TextureCoordinate)
            {
                if (element.Format != VertexElementFormat.Float2)
                    throw new NotSupportedException("Only float2 are supported for the texture coordinates");


                textureCoordinate = BitConverterExtensions.FromBytes<Vector2>(data);
            }

            if (element.Semantic == VertexElementSemantic.Color)
            {
                if (element.Format == VertexElementFormat.Float4) 
                    color = BitConverterExtensions.FromBytes<RgbaFloat>(data);
                if(element.Format != VertexElementFormat.Float3)
                {
                    var color3 = BitConverterExtensions.FromBytes<Vector3>(data);
                    color = new RgbaFloat(color3.X, color3.Y, color3.Z, 1);
                }
                else
                    throw new NotSupportedException("Only float3 and float4 are supported for the color");
            }
        }

        return new VertexPositionNormalTextureColor(position, color, textureCoordinate, normal);
    }

    public VertexPositionNormalTextureColor(VertexPositionNormalTexture vertex) 
        : this(vertex.Position, DefaultColor, vertex.TextureCoordinates, vertex.Normal) { }
    public VertexPositionNormalTextureColor(Vector3 position, RgbaFloat color)
        : this(position, color, Vector2.Zero) { }
    public VertexPositionNormalTextureColor(Vector3 position, RgbaFloat color, Vector2 textCoords)
        : this(position, color, textCoords, Vector3.Zero) { }
    public VertexPositionNormalTextureColor(Vector3 position, RgbaFloat color, Vector2 textCoords, Vector3 normal)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textCoords;
        Normal = normal;
    }

    public override int GetHashCode()
        => (Position, Normal, TextureCoordinate, Color).GetHashCode();

    public override string ToString()
        => $"Position: {Position}, Normal: {Normal}, TextureCoordinate:{TextureCoordinate}, Color: {Color}";

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        var objType = obj.GetType();
        if (objType != typeof(VertexPositionNormalTextureColor)) return false;
        return Equals((VertexPositionNormalTextureColor)obj);
    }

    public bool Equals(VertexPositionNormalTextureColor other)
        => Position == other.Position && Color == other.Color && TextureCoordinate == other.TextureCoordinate && Normal == other.Normal;
}
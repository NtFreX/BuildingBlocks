using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

internal struct TextVertex
{
    public const byte SizeInBytes = 20;

    public Vector2 Position;
    public Vector2 Cordinates;
    public RgbaByte Color;

    public TextVertex(Vector2 position, Vector2 cordinates, RgbaByte color)
    {
        Position = position;
        Cordinates = cordinates;
        Color = color;
    }
}


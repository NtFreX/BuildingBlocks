using SixLabors.Fonts;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

//TODO: use everywhere?
public struct TextData : IEquatable<TextData>
{
    public Font Font { get; init; }
    public string Value { get; init; }
    public RgbaFloat Color { get; init; }

    public TextData(Font font, string value, RgbaFloat color)
    {
        Font = font;
        Value = value;
        Color = color;
    }

    public static bool operator !=(TextData? one, TextData? two)
        => !(one == two);

    public static bool operator ==(TextData? one, TextData? two)
    {
        if (!one.HasValue && !two.HasValue)
            return true;
        if (!one.HasValue)
            return false;
        if (!two.HasValue)
            return false;
        return one.Equals(two);
    }

    public override int GetHashCode() => (Font, Value, Color).GetHashCode();

    public override string ToString()
        => $"Font: {Font}, Color: {Color}, Value: {Value}";

    public bool Equals(TextData other)
    {
        return other.Font == Font && other.Value == Value && other.Color == Color;
    }
}

using NtFreX.BuildingBlocks.Standard.Extensions;
using SixLabors.Fonts;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

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
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode() 
        => (Font, Value, Color).GetHashCode();

    public override string ToString()
        => $"Font: {Font}, Color: {Color}, Value: {Value}";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(TextData other)
        => other.Font == Font && other.Value == Value && other.Color == Color;
}

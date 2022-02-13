using SixLabors.Fonts;

namespace NtFreX.BuildingBlocks.Texture.Text;

public class TextAtlasData
{
    public string Characters { get; }
    public Font Font { get; }
    public FontRectangle Size { get; }

    public TextAtlasData(string characters, Font font)
    {
        Characters = characters;
        Font = font;
        Size = TextMeasurer.Measure(characters, new RendererOptions(font));
    }
}

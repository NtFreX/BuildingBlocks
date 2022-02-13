using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

public class TextAtlas
{
    private static readonly Dictionary<int, TextAtlas> TextAtlases = new Dictionary<int, TextAtlas>();

    public TextureView? Texture { get; private set; }
    public FontRectangle Size { get; private set; }
    public Font Font { get; }
    public Dictionary<char, FontRectangle> Characters { get; }
    public float LineHeight { get; private set; }

    private readonly string ControlCharacters = Environment.NewLine;
    private const float CharacterSpacing = 6f;
    private const string PreLoadText = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,.;:-_+*/=?^'()[]{} ";

    private TextAtlas(Font font)
    {
        Texture = null;
        Size = new FontRectangle();
        Font = font;
        Characters = new Dictionary<char, FontRectangle>();
    }

    public static TextAtlas ForFont(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Font font)
    {
        var key = (font.Name, font.Italic, font.Bold, font.Size).GetHashCode();
        if (!TextAtlases.TryGetValue(key, out var atlas))
        {
            atlas = new TextAtlas(font);
            atlas.Load(graphicsDevice, resourceFactory, PreLoadText);
            TextAtlases.Add(key, atlas);
        }

        return atlas;
    }

    public static void Load(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Font[] fonts, string text)
    {
        foreach(var font in fonts)
        {
            Load(graphicsDevice, resourceFactory, font, text);
        }
    }

    public static void Load(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Font font, string text)
    {
        ForFont(graphicsDevice, resourceFactory, font).Load(graphicsDevice, resourceFactory, text);
    }

    public void Load(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, string text)
    {
        var distinct = new string(text.Distinct().Where(x => !ControlCharacters.Any(cc => cc == x)).ToArray());
        var missing = distinct.Where(c => !Characters.ContainsKey(c)).ToArray();
        if (!missing.Any())
            return;

        var renderOptions = new RendererOptions(Font);
        var position = new PointF();
        var bounds = new SizeF();
        foreach (var character in missing)
        {
            var measure = TextMeasurer.Measure(new string(new[] { character }), renderOptions);
            var realMeasure = new FontRectangle(position.X, 0, measure.Width, measure.Height);

            bounds = new SizeF(
                Math.Max(realMeasure.X + realMeasure.Width, bounds.Width),
                Math.Max(realMeasure.Y + realMeasure.Height, bounds.Height));

            Characters.Add(character, realMeasure);
            position += new PointF(measure.Width + CharacterSpacing, 0);
        }

        // TODO: append to current texture instead of creating a new one
        Size = new FontRectangle(0, 0, bounds.Width, bounds.Height);
        Texture = TextureCreator.Create(graphicsDevice, resourceFactory, (int)Size.Width, (int)Size.Height, img =>
        {
            img.BackgroundColor(Color.Transparent);

            var position = new PointF();
            foreach(var character in Characters)
            {
                img.DrawText(character.Key.ToString(), Font, Color.White, position);
                position = new PointF(position.X + character.Value.Width + CharacterSpacing, 0);
            }
        });

        LineHeight = Characters.Max(x => x.Value.Height);
    }
}

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

public class TextAtlas
{
    private static readonly Dictionary<int, TextAtlas> TextAtlases = new ();

    public TextureView? Texture { get; private set; }
    public TextureView? AlphaTexture { get; private set; }
    public FontRectangle Size { get; private set; }
    public Font Font { get; }
    public Dictionary<char, FontRectangle> Characters { get; }
    public float LineHeight { get; private set; }

    private bool hasChanges = true;

    private readonly string ControlCharacters = Environment.NewLine;
    private readonly bool dither;
    private const float CharacterSpacing = 6f;
    private const string PreLoadText = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,.;:-_+*/=?^'()[]{} ";

    private TextAtlas(Font font, bool dither = true)
    {
        Texture = null;
        Size = new FontRectangle();
        Font = font;
        this.dither = dither;
        Characters = new Dictionary<char, FontRectangle>();
    }

    public static void DestroyAllDeviceResources()
    {
        foreach(var atlas in TextAtlases)
        {
            atlas.Value.DestroyDeviceResources();
        }
    }

    public static TextAtlas ForFont(Font font)
    {
        var key = (font.Name, font.Italic, font.Bold, font.Size).GetHashCode();
        if (!TextAtlases.TryGetValue(key, out var atlas))
        {
            atlas = new TextAtlas(font);
            atlas.Load(PreLoadText);
            TextAtlases.Add(key, atlas);
        }

        return atlas;
    }

    public static void Load(Font[] fonts, string text)
    {
        foreach(var font in fonts)
        {
            Load(font, text);
        }
    }

    public static void Load(Font font, string text)
    {
        ForFont(font).Load(text);
    }

    public void Load(string text)
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
            hasChanges = true;

            position += new PointF(measure.Width + CharacterSpacing, 0);
        }

        Size = new FontRectangle(0, 0, bounds.Width, bounds.Height);
        LineHeight = Font.LineHeight / renderOptions.DpiY * renderOptions.LineSpacing + Font.LineGap;

        DestroyDeviceResources();
    }

    internal void CreateDeviceResources(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if (!hasChanges)
            return;

        hasChanges = false;

        // TODO: append to current texture instead of creating a new one
        Texture = CreateTexture(graphicsDevice, resourceFactory, Color.Transparent, Color.Black);
        AlphaTexture = CreateTexture(graphicsDevice, resourceFactory, Color.Black, Color.White);
    }

    private TextureView CreateTexture(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Color background, Color foreground)
    {
        return TextureCreator.Create(graphicsDevice, resourceFactory, (int)Size.Width, (int)Size.Height, img =>
        {
            img.BackgroundColor(background);

            var position = new PointF();
            foreach (var character in Characters)
            {
                img.DrawText(character.Key.ToString(), Font, foreground, position);
                position = new PointF(position.X + character.Value.Width + CharacterSpacing, 0);
            }

            if(dither)
                img.BinaryDither(KnownDitherings.Atkinson);
        });
    }

    public void DestroyDeviceResources()
    {
        Texture?.Dispose();
        Texture = null;

        AlphaTexture?.Dispose();
        AlphaTexture = null;
    }
}

using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using System.Collections.Concurrent;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture
{
    public class TextureFactory : IDisposable
    {
        private readonly Game game;
        private readonly ILogger<TextureFactory> logger;
        private readonly ConcurrentDictionary<(string, TextureUsage), TextureView> textures = new ();

        private ProcessedTexture defaultTexture;
        private Dictionary<TextureUsage, TextureView> defaultTextureViews = new ();
        private ProcessedTexture emptyTexture;
        private Dictionary<TextureUsage, TextureView> emptyTextureViews = new ();

        public TextureFactory(Game game, ILogger<TextureFactory> logger)
        {
            this.game = game;
            this.logger = logger;

            defaultTexture = ProcessedTexture.Read(TextureCreator.CreateMissingTexture(SystemFonts.Find("Arial")));
            emptyTexture = ProcessedTexture.Read(TextureCreator.CreateEmptyTexture());
        }

        private (string, TextureUsage) GetTextureKey(string fullPath, TextureUsage usage)
            => (fullPath, usage);

        private async Task<TextureView> LoadTextureAsync(string fullPath, TextureUsage usage)
        {
            //TODO: better concurrent support

            var key = GetTextureKey(fullPath, usage);
            if (textures.TryGetValue(key, out var texture))
                return texture;

            var processedTexture = await ProcessedTexture.ReadAsync(fullPath);
            using var surfaceTexture = processedTexture.CreateDeviceTexture(game.GraphicsDevice, game.ResourceFactory, usage);
            var surfaceTextureView = game.ResourceFactory.CreateTextureView(surfaceTexture);

            textures.TryAdd(key, surfaceTextureView);

            return surfaceTextureView;


        }

        private TextureView GetFromCache(Dictionary<TextureUsage, TextureView> textures, TextureUsage usage, ProcessedTexture texture)
        {
            if (!textures.TryGetValue(usage, out var view))
            {
                using var surfaceTexture = texture.CreateDeviceTexture(game.GraphicsDevice, game.ResourceFactory, usage);
                view = game.ResourceFactory.CreateTextureView(surfaceTexture);
                textures.Add(usage, view);
            }
            return view;
        }

        public void SetDefaultTexture(string path)
        {
            defaultTexture = ProcessedTexture.Read(path);
            defaultTextureViews.Clear();
        }

        public void SetEmptyTexture(string path)
        {
            emptyTexture = ProcessedTexture.Read(path);
            emptyTextureViews.Clear();
        }

        public TextureView GetDefaultTexture(TextureUsage usage)
            => GetFromCache(defaultTextureViews, usage, defaultTexture);

        public TextureView GetEmptyTexture(TextureUsage usage)
            => GetFromCache(emptyTextureViews, usage, emptyTexture);

        public async Task<TextureView> GetTextureAsync(string? fullPath, TextureUsage usage)
        {
            if (File.Exists(fullPath))
            {
                return await LoadTextureAsync(fullPath, usage);
            }

            logger.LogWarning($"The texture with the path {fullPath} has not been found");
            logger.LogInformation($"The default texture will be used for {usage}");

            return GetDefaultTexture(usage);
        }

        public void Dispose()
        {
            foreach(var texture in defaultTextureViews.Values)
            {
                texture.Dispose();
            }
            foreach (var texture in emptyTextureViews.Values)
            {
                texture.Dispose();
            }
            foreach(var texture in textures.Values)
            {
                texture.Dispose();
            }
        }
    }
}

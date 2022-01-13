using Microsoft.Extensions.Logging;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture
{
    public class TextureFactory
    {
        private readonly Game game;
        private readonly ILogger<TextureFactory> logger;
        private readonly Dictionary<string, TextureView> textures = new Dictionary<string, TextureView>();

        private string? defaultTexturePath;
        private string? emptyTexture;

        public TextureFactory(Game game, ILogger<TextureFactory> logger)
        {
            this.game = game;
            this.logger = logger;
        }

        private string GetTextureKey(string fullPath, TextureUsage usage)
            => $"{fullPath}:{usage}";

        private async Task<TextureView> LoadTextureAsync(string fullPath, TextureUsage usage)
        {
            var key = GetTextureKey(fullPath, usage);
            if (textures.TryGetValue(key, out var texture))
                return texture;

            var processedTexture = await ProcessedTexture.ReadAsync(fullPath);
            using var surfaceTexture = processedTexture.CreateDeviceTexture(game.GraphicsDevice, game.ResourceFactory, usage);
            var surfaceTextureView = game.ResourceFactory.CreateTextureView(surfaceTexture);

            textures.Add(key, surfaceTextureView);
            return surfaceTextureView;
        }

        public void SetDefaultTexture(string path)
            => defaultTexturePath = path;

        public void SetEmptyTexture(string path)
            => emptyTexture = path;

        public Task<TextureView?> GetEmptyTextureAsync(TextureUsage usage)
            => GetTextureAsync(emptyTexture, usage);

        public async Task<TextureView?> GetTextureAsync(string? fullPath, TextureUsage usage)
        {
            if (File.Exists(fullPath))
            {
                return await LoadTextureAsync(fullPath, usage);
            }
            logger.LogWarning($"The texture with the path {fullPath} has not been found");
            if (!string.IsNullOrEmpty(defaultTexturePath))
            {
                logger.LogInformation($"The default texture {defaultTexturePath} will be used for {usage}");
                return await LoadTextureAsync(defaultTexturePath, usage);
            }
            return null;
        }
    }
}

using NtFreX.BuildingBlocks.Texture;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class TextureMaterialNode : MaterialNode
    {
        private readonly TextureProvider textureProvider;

        public TextureMaterialNode(TextureProvider textureProvider)
        {
            this.textureProvider = textureProvider;
        }

        public override async Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Output = await textureProvider.GetAsync(graphicsDevice, resourceFactory);
        }

        public override void DestroyDeviceResources()
        {
            Output?.Dispose();
            Output = null;
        }

        public override void Run(CommandList commandList, float delta) { }
    }
}

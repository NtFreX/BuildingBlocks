using Veldrid;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Material
{
    public abstract class MaterialNode
    {
        internal string MaterialName { get; set; }
        public TextureView? Input { get; internal set; }
        public TextureView? Output { get; protected set; }
        public VeldridTexture? OutputTexture { get; protected set; }

        public abstract Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory);
        public abstract void DestroyDeviceResources();
        public abstract void Run(CommandList commandList, float delta);
    }
}

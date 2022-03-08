using Veldrid;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Material
{
    public abstract class MaterialNode
    {
        public TextureView? Input { get; internal set; }
        public TextureView? Output { get; protected set; }
        public VeldridTexture? OutputTexture { get; protected set; }

        public abstract void CreateDeviceResources(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory);
        public abstract void DestroyDeviceResources();
        public abstract void Run(CommandList commandList, float delta);
    }
}

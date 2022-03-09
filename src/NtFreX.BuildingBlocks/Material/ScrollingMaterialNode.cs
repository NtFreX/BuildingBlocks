using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class ScrollingMaterialNode : MaterialNode
    {
        private readonly bool isDebug;
        private readonly float scrollX;
        private readonly float scrollY;
        private readonly uint computeX;
        private readonly uint computeY;

        private float ticks;
        private DeviceBuffer? scrollBuffer;
        private Shader? computeShader;
        private ResourceLayout? computeLayout;
        private Pipeline? computePipeline;
        private ResourceSet? computeResourceSet;

        public ScrollingMaterialNode(bool isDebug, float scrollX = 1f, float scrollY = 1f, uint computeX = 16, uint computeY = 16)
        {
            this.isDebug = isDebug;
            this.scrollX = scrollX;
            this.scrollY = scrollY;
            this.computeX = computeX;
            this.computeY = computeY;
        }

        public override Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(Input != null);

            computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, new Dictionary<string, string> { 
                { "width", Input.Target.Width.ToString() }, { "height", Input.Target.Height.ToString() },
                { "computeX", computeX.ToString() }, { "computeY", computeY.ToString() } }, "Resources/material/scrolling.cpt", isDebug);

            scrollBuffer = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            computeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("TexIn", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("TexOut", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ScrollBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            var computePipelineDesc = new ComputePipelineDescription(
                computeShader,
                computeLayout,
                computeX, computeY, 1);

            computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);

            OutputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                Input.Target.Width,
                Input.Target.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));

            Output = resourceFactory.CreateTextureView(OutputTexture);

            computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                computeLayout,
                Input, Output, scrollBuffer));

            return Task.CompletedTask;
        }

        public override void DestroyDeviceResources()
        {
            scrollBuffer?.Dispose();
            scrollBuffer = null;
            computeShader?.Dispose();
            computeShader = null;
            computeLayout?.Dispose();
            computeLayout = null;
            computePipeline?.Dispose();
            computePipeline = null;
            computeResourceSet?.Dispose();
            computeResourceSet = null;
        }

        public override void Run(CommandList commandList, float delta)
        {
            Debug.Assert(OutputTexture != null);
            Debug.Assert(Input != null);

            //TODO: move to resoruce update?
            ticks = ticks + delta / 1000f;
            var shifts = new Vector4(
                ticks * scrollX,
                ticks * scrollY, 0, 0);
            commandList.UpdateBuffer(scrollBuffer, 0, ref shifts);

            commandList.SetPipeline(computePipeline);
            commandList.SetComputeResourceSet(0, computeResourceSet);
            commandList.Dispatch(OutputTexture.Width / computeX, OutputTexture.Height / computeY, 1);
        }
    }
}

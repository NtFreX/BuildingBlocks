using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class WoodMaterialNode : MaterialNode
    {
        private readonly bool isDebug;
        private readonly uint computeX;
        private readonly uint computeY;

        private Shader? computeShader;
        private ResourceLayout? computeLayout;
        private Pipeline? computePipeline;
        private ResourceSet? computeResourceSet;

        public WoodMaterialNode(bool isDebug, uint computeX = 16, uint computeY = 16)
        {
            this.isDebug = isDebug;
            this.computeX = computeX;
            this.computeY = computeY;
        }

        public override Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(Input != null);

            computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, 
                new Dictionary<string, string> {
                    { "computeX", computeX.ToString() }, { "computeY", computeY.ToString() },
                    { "width", Input.Target.Width.ToString() }, { "height", Input.Target.Height.ToString() },
                    { "lineScale", "10.0" }, { "rotationX", "12" }, { "rotationY", "3" }, { "lineModifier", ".9" } }, "Resources/material/wood.cpt", isDebug);
            computeShader.Name = MaterialName + "_shiftinggmaterialnode_computeShader";

            computeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("TexIn", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("TexOut", ResourceKind.TextureReadWrite, ShaderStages.Compute)));
            computeLayout.Name = MaterialName + "_shiftinggmaterialnode_computeLayout";

            var computePipelineDesc = new ComputePipelineDescription(
                computeShader,
                computeLayout,
                computeX, computeY, 1);

            computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
            computePipeline.Name = MaterialName + "_shiftinggmaterialnode_computePipeline";

            OutputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                Input.Target.Width,
                Input.Target.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));
            OutputTexture.Name = MaterialName + "_shiftinggmaterialnode_OutputTexture";

            Output = resourceFactory.CreateTextureView(OutputTexture);
            Output.Name = MaterialName + "_shiftinggmaterialnode_Output";

            computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                computeLayout,
                Input, Output));
            computeResourceSet.Name = MaterialName + "_shiftinggmaterialnode_computeResourceSet";

            return Task.CompletedTask;
        }

        public override void DestroyDeviceResources()
        {
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

            commandList.SetPipeline(computePipeline);
            commandList.SetComputeResourceSet(0, computeResourceSet);
            commandList.Dispatch(OutputTexture.Width / computeX, OutputTexture.Height / computeY, 1);
        }
    }
}

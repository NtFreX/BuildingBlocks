using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class PerlinNoiseMaterialNode : MaterialNode
    {
        private readonly bool isDebug;
        private readonly uint computeX;
        private readonly uint computeY;

        private bool hasAllreadyRun = false;

        private Shader? computeShader;
        private ResourceLayout? computeLayout;
        private Pipeline? computePipeline;
        private ResourceSet? computeResourceSet;

        public PerlinNoiseMaterialNode(bool isDebug, uint computeX = 16, uint computeY = 16)
        {
            this.isDebug = isDebug;
            this.computeX = computeX;
            this.computeY = computeY;
        }

        public override Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(Input != null);

            computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, new Dictionary<string, string> { { "computeX", computeX.ToString() }, { "computeY", computeY.ToString() } }, "Resources/material/perlinnoise.cpt", isDebug);
            computeShader.Name = MaterialName + "_perlinmaterialnode_computeShader";

            computeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("TexIn", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("TexOut", ResourceKind.TextureReadWrite, ShaderStages.Compute)));
            computeLayout.Name = MaterialName + "_perlinmaterialnode_computeLayout";

            var computePipelineDesc = new ComputePipelineDescription(
                computeShader,
                computeLayout,
                computeX, computeY, 1);

            computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
            computePipeline.Name = MaterialName + "_perlinmaterialnode_computePipeline";

            OutputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                Input.Target.Width,
                Input.Target.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));
            OutputTexture.Name = MaterialName + "_perlinmaterialnode_OutputTexture";

            Output = resourceFactory.CreateTextureView(OutputTexture);
            Output.Name = MaterialName + "_perlinmaterialnode_Output";

            computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                computeLayout,
                Input, Output));
            computeResourceSet.Name = MaterialName + "_perlinmaterialnode_computeResourceSet";

            hasAllreadyRun = false;

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
            if (hasAllreadyRun)
                return;

            Debug.Assert(OutputTexture != null);

            commandList.SetPipeline(computePipeline);
            commandList.SetComputeResourceSet(0, computeResourceSet);
            commandList.Dispatch(OutputTexture.Width / computeX, OutputTexture.Height / computeY, 1);

            hasAllreadyRun = true;
        }
    }
}

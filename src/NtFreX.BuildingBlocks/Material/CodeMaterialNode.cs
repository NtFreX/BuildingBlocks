using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class CodeMaterialNode : MaterialNode
    {
        private readonly bool isDebug;
        private readonly uint computeX;
        private readonly uint computeY;
        private readonly string transformInputCordsCode;
        private readonly string transformVec4Code;
        private readonly string transformCouputCordsCode;

        private float ticks;
        private Shader? computeShader;
        private ResourceLayout? computeLayout;
        private Pipeline? computePipeline;
        private ResourceSet? computeResourceSet;

        public CodeMaterialNode(bool isDebug, uint computeX = 16, uint computeY = 16, string transformInputCordsCode = "", string transformVec4Code = "", string transformCouputCordsCode = "")
        {
            this.isDebug = isDebug;
            this.computeX = computeX;
            this.computeY = computeY;
            this.transformInputCordsCode = transformInputCordsCode;
            this.transformVec4Code = transformVec4Code;
            this.transformCouputCordsCode = transformCouputCordsCode;
        }

        public override Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(Input != null);

            computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, new Dictionary<string, string> {
                { "width", Input.Target.Width.ToString() }, { "height", Input.Target.Height.ToString() },
                { "computeX", computeX.ToString() }, { "computeY", computeY.ToString() }, 
                { "transformInputCordsCode", transformInputCordsCode }, { "transformVec4Code", transformVec4Code }, { "transformCouputCordsCode", transformCouputCordsCode } }, "Resources/material/code.cpt", isDebug);
            computeShader.Name = MaterialName + "_codematerialnode_shader";

            computeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("TexIn", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("TexOut", ResourceKind.TextureReadWrite, ShaderStages.Compute)));
            computeShader.Name = MaterialName + "_codematerialnode_computeLayout";

            var computePipelineDesc = new ComputePipelineDescription(
                computeShader,
                computeLayout,
                computeX, computeY, 1);

            computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
            computeShader.Name = MaterialName + "_codematerialnode_computePipeline";

            OutputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                Input.Target.Width,
                Input.Target.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));
            computeShader.Name = MaterialName + "_codematerialnode_OutputTexture";

            Output = resourceFactory.CreateTextureView(OutputTexture);
            computeShader.Name = MaterialName + "_codematerialnode_Output";

            computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                computeLayout,
                Input, Output));
            computeShader.Name = MaterialName + "_codematerialnode_computeResourceSet";

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
            Debug.Assert(Input != null);

            commandList.SetPipeline(computePipeline);
            commandList.SetComputeResourceSet(0, computeResourceSet);
            commandList.Dispatch(OutputTexture.Width / computeX, OutputTexture.Height / computeY, 1);
        }
    }
}

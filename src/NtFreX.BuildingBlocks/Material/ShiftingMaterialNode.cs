using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    public sealed class ShiftingMaterialNode : MaterialNode
    {
        private readonly bool isDebug;
        private readonly float redFactor;
        private readonly float blueFactor;
        private readonly float greenFactor;
        private readonly uint computeX;
        private readonly uint computeY;

        private float ticks;
        private DeviceBuffer? shiftBuffer;
        private Shader? computeShader;
        private ResourceLayout? computeLayout;
        private Pipeline? computePipeline;
        private ResourceSet? computeResourceSet;

        public ShiftingMaterialNode(bool isDebug, float redFactor = 1f, float blueFactor = 1f, float greenFactor = 1f, uint computeX = 16, uint computeY = 16)
        {
            this.isDebug = isDebug;
            this.redFactor = redFactor;
            this.blueFactor = blueFactor;
            this.greenFactor = greenFactor;
            this.computeX = computeX;
            this.computeY = computeY;
        }

        public override Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(Input != null);

            computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, new Dictionary<string, string> { { "computeX", computeX.ToString() }, { "computeY", computeY.ToString() } }, "Resources/material/shifting.cpt", isDebug);
            computeShader.Name = MaterialName + "_shiftingingmaterialnode_computeShader";

            shiftBuffer = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            shiftBuffer.Name = MaterialName + "_shiftingingmaterialnode_shiftBuffer";

            computeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("TexIn", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("TexOut", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ShiftBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            computeLayout.Name = MaterialName + "_shiftingingmaterialnode_computeLayout";

            var computePipelineDesc = new ComputePipelineDescription(
                computeShader,
                computeLayout,
                computeX, computeY, 1);

            computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
            computePipeline.Name = MaterialName + "_shiftingingmaterialnode_computePipeline";

            OutputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                Input.Target.Width,
                Input.Target.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));
            OutputTexture.Name = MaterialName + "_shiftingingmaterialnode_OutputTexture";

            Output = resourceFactory.CreateTextureView(OutputTexture);
            Output.Name = MaterialName + "_shiftingingmaterialnode_Output";

            computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                computeLayout,
                Input, Output, shiftBuffer));
            computeResourceSet.Name = MaterialName + "_shiftingingmaterialnode_computeResourceSet";

            return Task.CompletedTask;
        }

        public override void DestroyDeviceResources()
        {
            shiftBuffer?.Dispose();
            shiftBuffer = null;
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

            //TODO: move to resoruce update?
            ticks = ticks + delta / 1000f;
            var shifts = new Vector4(
                ticks * redFactor,
                ticks * greenFactor,
                ticks * blueFactor,
                0);
            commandList.UpdateBuffer(shiftBuffer, 0, ref shifts);

            commandList.SetPipeline(computePipeline);
            commandList.SetComputeResourceSet(0, computeResourceSet);
            commandList.Dispatch(OutputTexture.Width / computeX, OutputTexture.Height / computeY, 1);
        }
    }
}

using NtFreX.BuildingBlocks.Mesh.Common;
using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

internal class ScreenDuplicator : Renderable
{
    private DisposeCollector? disposeCollector;
    private Pipeline? pipeline;
    private DeviceBuffer? indexBuffer;
    private DeviceBuffer? vertexBuffer;

    private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    private readonly bool isDebug;

    public override RenderPasses RenderPasses => RenderPasses.Duplicator;

    public ScreenDuplicator(bool isDebug)
    {
        this.isDebug = isDebug;
    }

    public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
    {
        if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
            return false;

        Debug.Assert(renderContext.DuplicatorFramebuffer != null);

        var factory = new DisposeCollectorResourceFactory(graphicsDevice.ResourceFactory);
        disposeCollector = factory.DisposeCollector;

        var resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        (Shader vs, Shader fs) = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, new Dictionary<string, bool>(), new Dictionary<string, string>(), "Resources/dublicator", isDebug);

        var pd = new GraphicsPipelineDescription(
            new BlendStateDescription(
                RgbaFloat.Black,
                BlendAttachmentDescription.OverrideBlend,
                BlendAttachmentDescription.OverrideBlend),
            graphicsDevice.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
            RasterizerStateDescription.Default,
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                new[]
                {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[] { vs, fs, },
                ShaderPrecompiler.GetSpecializations(graphicsDevice)),
            new ResourceLayout[] { resourceLayout },
            renderContext.DuplicatorFramebuffer.OutputDescription);
        pipeline = factory.CreateGraphicsPipeline(ref pd);

        var verts = Quad.GetFullScreenQuadVerts(graphicsDevice.IsClipSpaceYInverted);

        vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(verts.Length * sizeof(float)), BufferUsage.VertexBuffer));
        commandList.UpdateBuffer(vertexBuffer, 0, verts);

        indexBuffer = factory.CreateBuffer(
            new BufferDescription((uint)s_quadIndices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        commandList.UpdateBuffer(indexBuffer, 0, s_quadIndices);

        return true;
    }

    public override void DestroyDeviceObjects()
    {
        disposeCollector?.DisposeAll();
    }

    public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
    {
        return new RenderOrderKey();
    }

    public override void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass)
    {
        commandList.SetPipeline(pipeline);
        commandList.SetGraphicsResourceSet(0, renderContext.MainSceneViewResourceSet);
        commandList.SetVertexBuffer(0, vertexBuffer);
        commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        commandList.DrawIndexed(6, 1, 0, 0, 0);
    }
}
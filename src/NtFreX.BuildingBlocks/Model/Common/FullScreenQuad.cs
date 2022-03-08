using NtFreX.BuildingBlocks.Mesh.Common;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

internal class FullScreenQuad : Renderable
{
    private DisposeCollector? disposeCollector;
    private Pipeline? pipeline;
    private DeviceBuffer? indexBuffer;
    private DeviceBuffer? vertexBuffer;

    // TODO: delete this together with second screen doublicator output?
    public bool UseTintedTexture { get; set; }

    public override RenderPasses RenderPasses => RenderPasses.SwapchainOutput;

    private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    private readonly bool isDebug;

    public FullScreenQuad(bool isDebug)
    {
        this.isDebug = isDebug;
    }

    public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
    {
        if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
            return false;

        var factory = new DisposeCollectorResourceFactory(graphicsDevice.ResourceFactory);
        disposeCollector = factory.DisposeCollector;

        var resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        (Shader vs, Shader fs) = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, new Dictionary<string, bool>(), new Dictionary<string, string>(), "Resources/fullscreen", isDebug);

        var pd = new GraphicsPipelineDescription(
            new BlendStateDescription(
                RgbaFloat.Black,
                BlendAttachmentDescription.OverrideBlend),
            DepthStencilStateDescription.Disabled,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                new[]
                {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[] { vs, fs },
                ShaderPrecompiler.GetSpecializations(graphicsDevice)),
            new ResourceLayout[] { resourceLayout },
            graphicsDevice.SwapchainFramebuffer.OutputDescription);
        pipeline = factory.CreateGraphicsPipeline(ref pd);

        float[] verts = Quad.GetFullScreenQuadVerts(graphicsDevice.IsClipSpaceYInverted);

        vertexBuffer = factory.CreateBuffer(new BufferDescription((uint) (verts.Length * sizeof(float)), BufferUsage.VertexBuffer));
        commandList.UpdateBuffer(vertexBuffer, 0, verts);

        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)s_quadIndices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        commandList.UpdateBuffer(indexBuffer, 0, s_quadIndices);


        //renderContext.MainSceneFramebuffer.GetOrCreateImGuiBinding();

        ////TODO: design something? remove screen doublicator?
        //var outputView = resourceFactory.CreateTextureView(renderContext.MainSceneDepthTexture.GetOrCreateImGuiBinding());
        //outputSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(renderContext.TextureSamplerResourceLayout, outputView, graphicsDevice.PointSampler));

        //graphicsDevice.

        return true;
    }

    //private ResourceSet outputSet;

    public override void DestroyDeviceObjects()
    {
        disposeCollector?.DisposeAll();
    }

    public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        => new RenderOrderKey();

    public override void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass)
    {
        commandList.SetPipeline(pipeline);
        //commandList.SetGraphicsResourceSet(0, outputSet);
        commandList.SetGraphicsResourceSet(0, UseTintedTexture ? renderContext.DuplicatorTargetSet1 : renderContext.DuplicatorTargetSet0);
        commandList.SetVertexBuffer(0, vertexBuffer);
        commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        commandList.DrawIndexed(6, 1, 0, 0, 0);
    }
}

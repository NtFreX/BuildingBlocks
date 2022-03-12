using NtFreX.BuildingBlocks.Mesh.Common;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

//TODO: move
internal class FullScreenQuad
{
    private DisposeCollector? disposeCollector;
    private Pipeline? pipeline;
    private DeviceBuffer? indexBuffer;
    private DeviceBuffer? vertexBuffer;

    // TODO: delete this together with second screen doublicator output?
    //public bool UseTintedTexture { get; set; }

    private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    private readonly bool isDebug;

    public FullScreenQuad(bool isDebug)
    {
        this.isDebug = isDebug;
    }

    public void CreateDeviceObjects(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList)
    {
        var factory = new DisposeCollectorResourceFactory(graphicsDevice.ResourceFactory);
        disposeCollector = factory.DisposeCollector;

        var resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        resourceLayout.Name = nameof(FullScreenQuad);

        (Shader vs, Shader fs) = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, new Dictionary<string, bool>(), new Dictionary<string, string>(), "Resources/fullscreen", isDebug);
        vs.Name = nameof(FullScreenQuad) + "vertex";
        fs.Name = nameof(FullScreenQuad) + "fragment";

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
        pipeline.Name = nameof(FullScreenQuad);

        float[] verts = Quad.GetFullScreenQuadVerts(graphicsDevice.IsClipSpaceYInverted);

        vertexBuffer = factory.CreateBuffer(new BufferDescription((uint) (verts.Length * sizeof(float)), BufferUsage.VertexBuffer));
        vertexBuffer.Name = nameof(FullScreenQuad);
        commandList.UpdateBuffer(vertexBuffer, 0, verts);

        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)s_quadIndices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        indexBuffer.Name = nameof(FullScreenQuad);
        commandList.UpdateBuffer(indexBuffer, 0, s_quadIndices);
    }

    public void DestroyDeviceObjects()
    {
        disposeCollector?.DisposeAll();
    }

    public void Render(CommandList commandList, ResourceSet outputSet)
    {
        commandList.SetPipeline(pipeline);
        commandList.SetGraphicsResourceSet(0, outputSet);
        commandList.SetVertexBuffer(0, vertexBuffer);
        commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        commandList.DrawIndexed(6, 1, 0, 0, 0);
    }
}

using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Standard;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

public class SkyboxRenderer : Renderable, IUpdateable
{
    private readonly Image<Rgba32> front;
    private readonly Image<Rgba32> back;
    private readonly Image<Rgba32> left;
    private readonly Image<Rgba32> right;
    private readonly Image<Rgba32> top;
    private readonly Image<Rgba32> bottom;

    private TextureView? textureView;
    private ResourceLayout? cameraLayout;
    private DeviceBuffer? vertexBuffer;
    private DeviceBuffer? indexBuffer;
    private DeviceBuffer? envBuffer;
    private Pipeline? pipeline;
    private ResourceSet? resourceSet;
    private ResourceSet? envResourceSet;

    private readonly DisposeCollector disposeCollector = new ();
    private readonly bool isDebug;
    private bool lightChanged = false;
    private bool cameraChanged = false;

    public SkyboxRenderer(Image<Rgba32> front, Image<Rgba32> back, Image<Rgba32> left, Image<Rgba32> right, Image<Rgba32> top, Image<Rgba32> bottom, bool isDebug = false)
    {
        this.front = front;
        this.back = back;
        this.left = left;
        this.right = right;
        this.top = top;
        this.bottom = bottom;
        this.isDebug = isDebug;
    }

    public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
    {
        if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
            return false;

        Debug.Assert(CurrentScene != null);
        Debug.Assert(CurrentResourceFactory != null);
        Debug.Assert(CurrentGraphicsDevice != null);
        Debug.Assert(CurrentRenderContext != null);
        Debug.Assert(CurrentRenderContext.MainSceneFramebuffer != null);

        cameraChanged = true;

        CurrentScene.Camera.ValueChanged += CameraChanged;
        CurrentScene.LightSystem.ValueChanged += LightSystemChanged;
        if (CurrentScene.LightSystem.Value != null)
            CurrentScene.LightSystem.Value.LightChanged += LightSystemLightChanged;

        vertexBuffer = CurrentResourceFactory.CreateBuffer(new BufferDescription((uint) (Unsafe.SizeOf<VertexPosition>() * vertices.Length), BufferUsage.VertexBuffer));
        commandList.UpdateBuffer(vertexBuffer, 0, vertices);

        indexBuffer = CurrentResourceFactory.CreateBuffer(new BufferDescription((uint)(Unsafe.SizeOf<ushort>() * indices.Length), BufferUsage.IndexBuffer));
        commandList.UpdateBuffer(indexBuffer, 0, indices);

        envBuffer = CurrentResourceFactory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<Vector4>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        commandList.UpdateBuffer(envBuffer, 0, scene.LightSystem.Value?.AmbientLight ?? Vector4.One);

        var imageSharpCubemapTexture = new ImageSharpCubemapTexture(right, left, top, bottom, back, front, false);

        var textureCube = imageSharpCubemapTexture.CreateDeviceTexture(CurrentGraphicsDevice, CurrentResourceFactory);
        textureView = CurrentResourceFactory.CreateTextureView(new TextureViewDescription(textureCube));

        var vertexLayouts = new VertexLayoutDescription[] 
        {
            new VertexLayoutDescription(new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
        };

        var shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(CurrentGraphicsDevice, CurrentResourceFactory, 
            new Dictionary<string, bool> { { "hasLights", scene.LightSystem.Value != null } }, 
            new Dictionary<string, string>(), "Resources/skybox", isDebug);

        cameraLayout = CurrentResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("CubeTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("CubeSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        var envLayout = CurrentResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Material", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

        var pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            graphicsDevice.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, new[] { shaders.VertexShader, shaders.FragementShader }, ShaderPrecompiler.GetSpecializations(CurrentGraphicsDevice)),
            new ResourceLayout[] { cameraLayout, envLayout },
            CurrentRenderContext.MainSceneFramebuffer.OutputDescription);

        pipeline = CurrentResourceFactory.CreateGraphicsPipeline(ref pd);

        envResourceSet = CurrentResourceFactory.CreateResourceSet(new ResourceSetDescription(envLayout, envBuffer));

        disposeCollector.Add(vertexBuffer, indexBuffer, textureCube, pipeline, envResourceSet, shaders.VertexShader, shaders.FragementShader, textureView, cameraLayout);

        return true;
    }

    private void CameraChanged(object? sender, Cameras.Camera? e)
        => cameraChanged = true;
    private void LightSystemChanged(object? sender, LightSystem? e)
        => lightChanged = true;
    private void LightSystemLightChanged(object? sender, EventArgs e)
        => lightChanged = true;

    public override void DestroyDeviceObjects()
    {
        Debug.Assert(CurrentScene != null);

        CurrentScene.Camera.ValueChanged -= CameraChanged;
        CurrentScene.LightSystem.ValueChanged -= LightSystemChanged;
        if (CurrentScene.LightSystem.Value != null)
            CurrentScene.LightSystem.Value.LightChanged -= LightSystemLightChanged;

        base.DestroyDeviceObjects();

        resourceSet?.Dispose();
        resourceSet = null;

        disposeCollector.DisposeAll();
    }

    //TODO: do we need the rendercontext here? only in resource creation? also rename output context or whatever
    public override void Render(GraphicsDevice gd, CommandList cl, RenderContext sc, RenderPasses renderPass)
    {
        cl.SetPipeline(pipeline);
        cl.SetGraphicsResourceSet(0, resourceSet);
        cl.SetGraphicsResourceSet(1, envResourceSet);
        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.DrawIndexed((uint)indices.Length, 1, 0, 0, 0);
    }

    public override RenderPasses RenderPasses => RenderPasses.Standard;
    public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition) => new (ulong.MaxValue);

    public void Update(float deltaSeconds, InputHandler inputHandler)
    {
        if (lightChanged && CurrentGraphicsDevice != null && CurrentScene != null)
        {
            CurrentGraphicsDevice.UpdateBuffer(envBuffer, 0, CurrentScene.LightSystem.Value?.AmbientLight ?? Vector4.One);
            lightChanged = false;
        }

        if(cameraChanged && CurrentGraphicsDevice != null && CurrentResourceFactory != null && CurrentScene?.Camera.Value != null)
        {
            if (resourceSet != null)
                resourceSet.Dispose();

            resourceSet = CurrentResourceFactory.CreateResourceSet(new ResourceSetDescription(
                cameraLayout,
                CurrentScene.Camera.Value.ProjectionBuffer,
                CurrentScene.Camera.Value.ViewBuffer,
                textureView,
                CurrentGraphicsDevice.PointSampler));
            cameraChanged = false;
        }
    }

    private static readonly VertexPosition[] vertices = new VertexPosition[]
    {
        // Top
        new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
        new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
        // Bottom
        new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
        new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
        // Left
        new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
        new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
        // Right
        new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
        new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
        // Back
        new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
        new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
        // Front
        new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
        new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
        new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
        new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
    };

    private static readonly ushort[] indices = new ushort[]
    {
        0,1,2, 0,2,3,
        4,5,6, 4,6,7,
        8,9,10, 8,10,11,
        12,13,14, 12,14,15,
        16,17,18, 16,18,19,
        20,21,22, 20,22,23,
    };
}

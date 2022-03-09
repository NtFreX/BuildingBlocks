using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

public interface IBoundsBuffer
{
    BoundingBox GetBoundingBox();
    Vector3 GetCenter();
}
public interface IResetBuffer { }

public struct ParticleNullBounds : IBoundsBuffer
{
    public BoundingBox GetBoundingBox()
        => new BoundingBox();

    public Vector3 GetCenter()
        => Vector3.Zero;
}

public struct ParticleNullReset : IResetBuffer { }

public struct ParticleBoxBounds : IBoundsBuffer
{
    public Vector3 BoundingBoxMin;
    private float _padding0;
    public Vector3 BoundingBoxMax;
    private float _padding1;

    public BoundingBox GetBoundingBox()
        => new BoundingBox(BoundingBoxMin, BoundingBoxMax);
    public Vector3 GetCenter()
        => GetBoundingBox().GetCenter();
}

public struct ParticleBoxReset : IResetBuffer
{
    public Vector3 ResetBoxMin;
    private float _padding2;
    public Vector3 ResetBoxMax;
    private float _padding3;
}

public struct ParticleSphereBounds : IBoundsBuffer
{
    public Vector3 Position;
    public float Radius;

    public BoundingBox GetBoundingBox()
        => new BoundingBox(Position - Vector3.One * Radius, Position + Vector3.One * Radius);
    public Vector3 GetCenter()
        => Position;
}

public struct ParticleSphereReset : IResetBuffer
{
    public Vector3 ResetPosition;
    public float ResetRadius;
}

public struct ParticleCircleReset : IResetBuffer
{
    public Vector3 ResetPosition;
    public float ResetRadius;
}

public struct ParticleInfo
{
    public Vector3 Position;
    public float Scale;
    public Vector3 Velocity;
    public float LivetimeModifer;
    public Vector4 Color;
    public Vector4 ColorModifier;
    public Vector4 InitialColor;
    public Vector3 TexCoords;
    public float Livetime;
    public Vector3 InitialVelocity;
    private float _padding0 = 0;
    public Vector3 VelocityModifier;
    private float _padding1 = 0;

    public ParticleInfo(Vector3 position, float scale, Vector3 velocity, Vector4 color, Vector4? colorModifier = null, Vector3? velocityModifier = null, float liveTime = 1f, float liveTimeModifier = 0f, Vector3? texCoords = null)
    {
        Position = position;
        Scale = scale;
        Velocity = velocity;
        InitialVelocity = velocity;
        VelocityModifier = velocityModifier ?? Vector3.Zero;
        Color = color;
        InitialColor = color;
        Livetime = liveTime;
        LivetimeModifer = liveTimeModifier;
        ColorModifier = colorModifier ?? Vector4.Zero;
        TexCoords = texCoords ?? Vector3.Zero;
    }
}

public class ParticleRenderer : ParticleRenderer<ParticleNullBounds, ParticleNullReset>
{
    public ParticleRenderer(Transform transform, ParticleInfo[] particles, TextureProvider? textureProvider = null, bool isDebug = false) 
        : base(transform, particles, new ParticleNullBounds(), new ParticleNullReset(), textureProvider, isDebug)
    { }
}

//TODO: fix reset location and bounds (probably should not be transformed befroe passing to cpt shader)
public class ParticleRenderer<TBounds, TReset> : CullRenderable
    where TBounds : unmanaged, IBoundsBuffer
    where TReset : unmanaged, IResetBuffer
{
    private TextureView? textureView;
    private DeviceBuffer? worldBuffer;
    private DeviceBuffer? boundsBuffer;
    private DeviceBuffer? resetBuffer;
    private DeviceBuffer? particleBuffer;
    private DeviceBuffer? particleSizeBuffer;
    private Shader? computeShader;
    private Pipeline? computePipeline;
    private Pipeline? graphicsPipeline;
    private ResourceSet? graphicsParticleResourceSet;
    private ResourceSet? graphicsWorldResourceSet;
    private ResourceSet? computeResourceSet;
    private ResourceSet? computeSizeResourceSet;
    private ResourceSet? computeBoundsSet;
    private ResourceSet? computeResetSet;
    private ResourceSet? textureResourceSet;

    private bool hasParticlesChanged = true;
    private bool hasboundsChanged = true;
    private bool hasResetChanged = true;
    private ParticleInfo[] particles;
    private TBounds bounds;
    private TReset reset;

    private readonly uint maxParticles;
    private readonly TextureProvider? textureProvider;
    private readonly bool isDebug;

    public Transform Transform { get; }
    public override RenderPasses RenderPasses => RenderPasses.Particles;

    //TODO: add spacer similar to meshrenderer? generalize
    //TODO: cache this
    public override BoundingBox GetBoundingBox() => BoundingBox.Transform(bounds.GetBoundingBox(), Transform.CreateWorldMatrix());
    public override Vector3 GetCenter() => GetBoundingBox().GetCenter();
    public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
    {
        Debug.Assert(CurrentScene?.Camera.Value != null);

        return RenderOrderKey.Create(
                0,
                Vector3.Distance(GetCenter(), cameraPosition),
                CurrentScene.Camera.Value.FarDistance);
    }
    public ParticleRenderer(Transform transform, ParticleInfo[] particles, TBounds bounds, TReset reset, TextureProvider? textureProvider = null, bool isDebug = false)
    {
        this.isDebug = isDebug;
        this.maxParticles = (uint) particles.Length;
        this.particles = particles;
        this.bounds = bounds;
        this.reset = reset;
        this.textureProvider = textureProvider;

        Transform = transform;
    }

    //public void SetBounds(BoundingBox bounds, BoundingBox reset)
    //{
    //    this.bounds = bounds;
    //    this.reset = reset;
    //    this.center = bounds.GetDimensions();
    //    hasboundsChanged = true;
    //}

    private bool HasBounds() => typeof(TBounds) != typeof(ParticleNullBounds);
    private bool HasReset() => typeof(TReset) != typeof(ParticleNullReset);

    public void SetParticles(ParticleInfo[] particles)
    {
        if (particles.Length > maxParticles)
            throw new Exception("To many particles for this system");

        var padded = new List<ParticleInfo>(particles);
        if (particles.Length < maxParticles)
            padded.AddRange(Enumerable.Repeat(new ParticleInfo(), (int) (maxParticles - particles.Length)));
        
        this.particles = padded.ToArray();

        hasParticlesChanged = true;
    }

    public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
    {
        if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
            return false;

        Debug.Assert(renderContext.MainSceneFramebuffer != null);

        particleBuffer = resourceFactory.CreateBuffer(
            new BufferDescription(
                (uint)(Unsafe.SizeOf<ParticleInfo>() * maxParticles),
                BufferUsage.StructuredBufferReadWrite,
                (uint)Unsafe.SizeOf<ParticleInfo>()));

        worldBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<Matrix4x4>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        commandList.UpdateBuffer(worldBuffer, 0, Transform.CreateWorldMatrix()); // TODO: make dynamic

        particleSizeBuffer = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));

        computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { 
            { "hasBoundingBox", typeof(TBounds) == typeof(ParticleBoxBounds) }, { "hasBoundingSphere", typeof(TBounds) == typeof(ParticleSphereBounds) },
            { "hasResetBox", typeof(TReset) == typeof(ParticleBoxReset) }, { "hasResetSphere", typeof(TReset) == typeof(ParticleSphereReset) }, { "hasResetCircle", typeof(TReset) == typeof(ParticleCircleReset) } }, 
            new Dictionary<string, string> { { "resetSet", !HasBounds() ? "2" : "3" } }, "Resources/particle.cpt", isDebug);

        var particleStorageLayoutCompute = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute)));

        var computeSizeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

        var computeLayouts = new List<ResourceLayout>(new[] { particleStorageLayoutCompute, computeSizeLayout });

        if (HasBounds())
        {
            var boundsLayout = ResourceLayoutFactory.GetParticleBoundsLayout(resourceFactory);
            boundsBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<TBounds>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            computeBoundsSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(boundsLayout, boundsBuffer));
            computeLayouts.Add(boundsLayout);
        }
        if (HasReset())
        {
            var resetLayout = ResourceLayoutFactory.GetParticleResetLayout(resourceFactory);
            resetBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<TReset>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            computeResetSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(resetLayout, resetBuffer));
            computeLayouts.Add(resetLayout);
        }

        var computePipelineDesc = new ComputePipelineDescription(
            computeShader,
            computeLayouts.ToArray(),
            1, 1, 1);

        computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
        computeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(particleStorageLayoutCompute, particleBuffer));
        computeSizeResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(computeSizeLayout, particleSizeBuffer));

        var shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, 
            new Dictionary<string, bool> { { "hasTexture", textureProvider != null } },
            new Dictionary<string, string> { { "viewProjectionSet", "1" }, { "worldSet", "2" }, { "cameraInfoSet", "3" } }, "Resources/particle", isDebug);

        var shaderSet = new ShaderSetDescription(
            Array.Empty<VertexLayoutDescription>(),
            new[] { shaders.VertexShader, shaders.FragementShader });

        var worldLayout = ResourceLayoutFactory.GetWorldLayout(resourceFactory);
        var particleStorageLayoutGrapcis = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex)));

        //TODO: draw to seperate texture!!!!
        var layouts = new List<ResourceLayout>(new[] { particleStorageLayoutGrapcis, ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory), worldLayout, ResourceLayoutFactory.GetCameraInfoVertexLayout(resourceFactory) });
        if (textureProvider != null)
        {
            var textureLayout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);
            layouts.Add(textureLayout);

            textureView = await textureProvider.GetAsync(graphicsDevice, resourceFactory);
            textureResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, textureView, graphicsDevice.PointSampler));
        }
        var particleDrawPipelineDesc = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            graphicsDevice.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
            RasterizerStateDescription.Default,
            PrimitiveTopology.PointList,
            shaderSet,
            layouts.ToArray(),
            renderContext.MainSceneFramebuffer.OutputDescription);

        graphicsPipeline = resourceFactory.CreateGraphicsPipeline(ref particleDrawPipelineDesc);
        graphicsParticleResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(particleStorageLayoutGrapcis, particleBuffer));
        graphicsWorldResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(worldLayout, worldBuffer));

        return true;
    }

    public override void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass)
    {
        //TODO: do this in resource update
        if (hasParticlesChanged)
        {
            commandList.UpdateBuffer(particleBuffer, 0, particles);
            commandList.UpdateBuffer(particleSizeBuffer, 0, new uint[] { (uint)particles.Length, 0, 0, 0 });
            hasParticlesChanged = false;
        }
        if (hasboundsChanged)
        {
            Debug.Assert(boundsBuffer != null);
            Debug.Assert(HasBounds());

            commandList.UpdateBuffer(boundsBuffer, 0, bounds);
            hasboundsChanged = false;
        }
        if (hasResetChanged)
        {
            Debug.Assert(resetBuffer != null);
            Debug.Assert(HasReset());

            commandList.UpdateBuffer(resetBuffer, 0, reset);
            hasboundsChanged = false;
        }

        Debug.Assert(CurrentScene?.Camera.Value?.ProjectionViewResourceSet != null);

        commandList.SetPipeline(computePipeline);
        commandList.SetComputeResourceSet(0, computeResourceSet);
        commandList.SetComputeResourceSet(1, computeSizeResourceSet);
        if(HasBounds())
            commandList.SetComputeResourceSet(2, computeBoundsSet);
        if (HasReset())
            commandList.SetComputeResourceSet(!HasBounds() ? 2u : 3u, computeResetSet);
        commandList.Dispatch(maxParticles, 1, 1);
        
        //TODO: draw to seperate texture?
        commandList.SetFramebuffer(renderContext.MainSceneFramebuffer);
        commandList.SetFullViewports();
        commandList.SetFullScissorRects();
        commandList.SetPipeline(graphicsPipeline);
        commandList.SetGraphicsResourceSet(0, graphicsParticleResourceSet);
        commandList.SetGraphicsResourceSet(1, CurrentScene.Camera.Value.ProjectionViewResourceSet);
        commandList.SetGraphicsResourceSet(2, graphicsWorldResourceSet);
        commandList.SetGraphicsResourceSet(3, CurrentScene.Camera.Value.CameraInfoResourceSet);
        if (textureProvider != null)
            commandList.SetGraphicsResourceSet(4, textureResourceSet);

        commandList.Draw(maxParticles, 1, 0, 0);
    }

    public override void DestroyDeviceObjects()
    {
        computeResetSet?.Dispose();
        computeResetSet = null;

        resetBuffer?.Dispose();
        resetBuffer = null;

        worldBuffer?.Dispose();
        worldBuffer = null;

        particleBuffer?.Dispose();
        particleBuffer = null;

        particleSizeBuffer?.Dispose();
        particleSizeBuffer = null;

        computeShader?.Dispose();
        computeShader = null;

        computePipeline?.Dispose();
        computePipeline = null;

        graphicsPipeline?.Dispose();
        graphicsPipeline = null;

        graphicsParticleResourceSet?.Dispose();
        graphicsParticleResourceSet = null;

        graphicsWorldResourceSet?.Dispose();
        graphicsWorldResourceSet = null;
        
        computeResourceSet?.Dispose();
        computeResourceSet = null;

        computeSizeResourceSet?.Dispose();
        computeSizeResourceSet = null;

        textureView?.Dispose();
        textureView = null;

        textureResourceSet?.Dispose();
        textureResourceSet = null;
    }
}

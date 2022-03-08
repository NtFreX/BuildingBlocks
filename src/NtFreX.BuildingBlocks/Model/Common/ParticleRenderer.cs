using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

//TODO: fix reset location and bounds (probably should not be transformed befroe passing to cpt shader)
public class ParticleRenderer : CullRenderable
{
    public struct ParticleBounds
    {
        public Vector3 BoundingBoxMin;
        private float _padding0;
        public Vector3 BoundingBoxMax;
        private float _padding1;
        public Vector3 ResetBoxMin;
        private float _padding2;
        public Vector3 ResetBoxMax;
        private float _padding3;
    }

    public struct ParticleInfo
    {
        public Vector3 Position;
        public float Scale;
        public Vector3 Velocity;
        private float _padding0 = 0;
        public Vector4 Color;
        public Vector3 TexCoords;
        private float _padding1 = 0;

        public ParticleInfo(Vector3 position, float scale, Vector3 velocity, Vector4 color, Vector3? texCoords = null)
        {
            Position = position;
            Scale = scale;
            Velocity = velocity;
            Color = color;
            TexCoords = texCoords ?? Vector3.Zero;
        }
    }

    private TextureView? textureView;
    private DeviceBuffer? worldBuffer;
    private DeviceBuffer? boundsBuffer;
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
    private ResourceSet? textureResourceSet;

    private bool hasParticlesChanged = true;
    private bool hasboundsChanged = true;
    private ParticleInfo[] particles;
    private BoundingBox bounds;
    private BoundingBox reset;
    private Vector3 center;

    private readonly uint maxParticles;
    private readonly TextureProvider textureProvider;
    private readonly bool isDebug;

    public Transform Transform { get; }
    public override RenderPasses RenderPasses => RenderPasses.Particles;

    //TODO: add spacer similar to meshrenderer? generalize
    public override BoundingBox GetBoundingBox() => bounds;
    public override Vector3 GetCenter() => center;
    public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
    {
        Debug.Assert(CurrentScene?.Camera.Value != null);

        return RenderOrderKey.Create(
                0,
                Vector3.Distance(GetCenter(), cameraPosition),
                CurrentScene.Camera.Value.FarDistance);
    }
    public ParticleRenderer(Transform transform, ParticleInfo[] particles, BoundingBox bounds, BoundingBox reset, TextureProvider textureProvider = null, bool isDebug = false)
    {
        this.isDebug = isDebug;
        this.maxParticles = (uint) particles.Length;
        this.particles = particles;
        this.bounds = BoundingBox.Transform(bounds, transform.CreateWorldMatrix());
        this.reset = reset;
        this.center = bounds.GetDimensions();
        this.textureProvider = textureProvider;

        Transform = transform;
    }

    public void SetBounds(BoundingBox bounds, BoundingBox reset)
    {
        this.bounds = BoundingBox.Transform(bounds, Transform.CreateWorldMatrix());
        this.reset = reset;
        this.center = bounds.GetDimensions();
        hasboundsChanged = true;
    }

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

        computeShader = ShaderPrecompiler.CompileComputeShader(graphicsDevice, resourceFactory, new Dictionary<string, bool> { }, new Dictionary<string, string> { }, "Resources/particle.cpt", isDebug);

        var boundsLayout = ResourceLayoutFactory.GetParticleBoundsLayout(resourceFactory);
        boundsBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<ParticleBounds>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        computeBoundsSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(boundsLayout, boundsBuffer));

        var particleStorageLayoutCompute = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute)));

        var computeSizeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

        var computePipelineDesc = new ComputePipelineDescription(
            computeShader,
            new[] { particleStorageLayoutCompute, computeSizeLayout, boundsLayout },
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
            commandList.UpdateBuffer(boundsBuffer, 0, new ParticleBounds { BoundingBoxMax = bounds.Max, BoundingBoxMin = bounds.Min, ResetBoxMax = reset.Max, ResetBoxMin = reset.Min });
            hasboundsChanged = false;
        }

        Debug.Assert(CurrentScene?.Camera.Value?.ProjectionViewResourceSet != null);

        commandList.SetPipeline(computePipeline);
        commandList.SetComputeResourceSet(0, computeResourceSet);
        commandList.SetComputeResourceSet(1, computeSizeResourceSet);
        commandList.SetComputeResourceSet(2, computeBoundsSet);
        commandList.Dispatch(maxParticles, 1, 1);
        
        //TODO: draw to seperate texture!!!!
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

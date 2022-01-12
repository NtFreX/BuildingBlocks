using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public class Model : IRenderable
    {
        private readonly ResourceSet projectionViewWorldResourceSet;
        private readonly ResourceSet cameraInfoResourceSet;
        private readonly ResourceSet lightInfoResourceSet;
        private readonly ResourceSet materialInfoResourceSet;
        private readonly ResourceSet surfaceTextureResourceSet;
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Shader[] shaders;
        private readonly InstanceInfo[] instances;
        private readonly BoundingBox instanceBoundingBox;
        private readonly DeviceBuffer instanceVertexBuffer;
        private readonly Cached<BoundingBox> boundingBoxCache;
        private readonly Cached<Vector3> centerCache;
        private readonly List<IBehavior> behaviors = new List<IBehavior>();

        public readonly IMutable<Vector3> Position;
        public readonly IMutable<Quaternion> Rotation;
        public readonly IMutable<Vector3> Scale;
        public readonly IMutable<MaterialInfo> Material;
        public readonly Mutable<PolygonFillMode> FillMode = new Mutable<PolygonFillMode>(PolygonFillMode.Solid);

        public string? Name { get; set; }

        public MeshDeviceBuffer MeshBuffer { get; private set; }
        public DeviceBuffer WorldBuffer { get; private set; }
        public DeviceBuffer MaterialInfoBuffer { get; private set; }
        public Matrix4x4 WorldMatrix { get; private set; }

        public BoundingBox GetBoundingBox() => boundingBoxCache.Get();
        public Vector3 GetCenter() => centerCache.Get();

        private Pipeline? pipeline;
        private bool hasWorldChanged = true;
        private bool hasMaterialChanged = true;
        private bool wasTransparent = false;
        private Vector3 position = Vector3.Zero;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public unsafe Model(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, ModelCreationInfo creationInfo, Shader[] shaders,
            MeshDeviceBuffer meshBuffer, string? name = null, InstanceInfo[]? instances = null, IBehavior[]? behaviors = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.graphicsSystem = graphicsSystem;
            this.shaders = shaders;
            this.MeshBuffer = meshBuffer;

            if (behaviors != null)
            {
                this.behaviors.AddRange(behaviors);
            }

            Material = new MutableWrapper<MaterialInfo>(() => this.MeshBuffer.Material, material => this.MeshBuffer.Material = material);
            Position = new MutableWrapper<Vector3>(() => this.position, position => this.position = position);
            Rotation = new MutableWrapper<Quaternion>(() => this.rotation, rotation => this.rotation = rotation);
            Scale = new MutableWrapper<Vector3>(() => this.scale, scale => this.scale = scale);
            WorldBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            Name = name;

            //TODO: move to mesh
            MaterialInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<MaterialInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Scale.ValueChanged += (_, _) => InvalidateWorldCache();
            Position.ValueChanged += (_, _) => InvalidateWorldCache();
            Rotation.ValueChanged += (_, _) => InvalidateWorldCache();
            FillMode.ValueChanged += (_, _) => UpdatePipeline();
            Material.ValueChanged += (_, _) => UpdateMaterialInfo();

            position = creationInfo.Position;
            scale = creationInfo.Scale;
            rotation = creationInfo.Rotation;
            wasTransparent = Material.Value.Opacity != 1f;

            var projectionViewWorldLayout = ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory);
            this.projectionViewWorldResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(projectionViewWorldLayout, graphicsSystem.Camera.ProjectionBuffer, graphicsSystem.Camera.ViewBuffer, WorldBuffer));

            var cameraInfoLayout = ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory);
            this.cameraInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(cameraInfoLayout, graphicsSystem.Camera.CameraInfoBuffer));

            var lightInfoLayout = ResourceLayoutFactory.GetLightInfoLayout(resourceFactory);
            this.lightInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(lightInfoLayout, graphicsSystem.LightSystem.LightBuffer));

            var materialLayout = ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory);
            this.materialInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(materialLayout, MaterialInfoBuffer));

            // TODO: support models without texture (render passes?)
            var surfaceTextureLayout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);
            this.surfaceTextureResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(surfaceTextureLayout, meshBuffer.TextureView, graphicsDevice.Aniso4xSampler));

            this.instances = instances ?? new[] { new InstanceInfo() };
            instanceBoundingBox = this.instances
                .Select(x => BoundingBox.Transform(meshBuffer.BoundingBox, CreateWorldMatrix(x.Position, Matrix4x4.CreateRotationX(x.Rotation.X) * Matrix4x4.CreateRotationY(x.Rotation.Y) * Matrix4x4.CreateRotationZ(x.Rotation.Z), x.Scale)))
                .Aggregate((one, two) => BoundingBox.Combine(one, two));
            instanceVertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint) (InstanceInfo.Size * this.instances.Length), BufferUsage.VertexBuffer));
            graphicsDevice.UpdateBuffer(instanceVertexBuffer, 0, this.instances);

            boundingBoxCache = new Cached<BoundingBox>(() => BoundingBox.Transform(instanceBoundingBox, WorldMatrix));
            centerCache = new Cached<Vector3>(() => GetBoundingBox().GetCenter());

            InvalidateWorldCache();
            UpdateMaterialInfo();
            UpdatePipeline();
            Update(graphicsDevice, InputHandler.Empty, 0f);
        }

        public static unsafe Model Create<TVertex, TIndex>(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem,
            ModelCreationInfo creationInfo, Shader[] shaders, MeshDataProvider<TVertex, TIndex> mesh,
            TextureView? textureView = null, string? name = null)
                where TVertex : unmanaged
                where TIndex : unmanaged
        {
            var data = MeshDeviceBuffer.Create(graphicsDevice, resourceFactory, mesh, textureView: textureView);
            return new Model(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders, data, name: name);
        }

        public static unsafe Model Create<TVertex, TIndex, TShape>(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem,
            ModelCreationInfo creationInfo, Shader[] shaders, MeshDataProvider<TVertex, TIndex> mesh, Func<Simulation, TShape> shapeAllocator,
            TextureView? textureView = null, string? name = null)
                where TVertex : unmanaged
                where TIndex : unmanaged
                where TShape : unmanaged, IShape
        {
            var data = PhysicsMeshDeviceBuffer<TShape>.Create(graphicsDevice, resourceFactory, mesh, shapeAllocator, textureView: textureView);
            return new Model(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders, data, name: name);
        }

        public static unsafe Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, ModelCreationInfo creationInfo, Shader[] shaders, 
            MeshData mesh, VertexLayoutDescription vertexLayout, IndexFormat indexFormat, PrimitiveTopology primitiveTopology, 
            TextureView? textureView, MaterialInfo? material = null, string? name = null)
        {
            var boundingBox = mesh.GetBoundingBox();
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory);
            var data = new MeshDeviceBuffer(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, boundingBox, vertexLayout, indexFormat, primitiveTopology, material, textureView: textureView/*, triangles: triangles*/);

            return new Model(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders, data, name: name);
        }

        private static Matrix4x4 CreateWorldMatrix(Vector3 position, Matrix4x4 rotation, Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale) *
                   rotation *
                   Matrix4x4.CreateTranslation(position);
        }

        private void InvalidateWorldCache()
        {
            WorldMatrix = CreateWorldMatrix(Position.Value, Matrix4x4.CreateFromQuaternion(Rotation.Value), Scale.Value);
            centerCache?.Invalidate(); 
            boundingBoxCache?.Invalidate();
            hasWorldChanged = true;
        }

        private void UpdateMaterialInfo()
        {
            if ((Material.Value.Opacity == 1f && wasTransparent) || (Material.Value.Opacity < 1f && !wasTransparent))
            {
                UpdatePipeline();
            }

            hasMaterialChanged = true;
            wasTransparent = Material.Value.Opacity != 1f;
        }

        private void UpdatePipeline()
        {
            var layouts = new List<ResourceLayout>();
            layouts.Add(ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetLightInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory));

            var blendState = Material.Value.Opacity == 1f ? BlendStateDescription.SingleOverrideBlend : BlendStateDescription.SingleAlphaBlend;

            VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(
                new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));
            vertexLayoutPerInstance.InstanceStepRate = 1;

            var shaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { MeshBuffer.VertexLayout, vertexLayoutPerInstance },
                shaders: shaders);

            this.pipeline = GraphicsPipelineFactory.GetGraphicsPipeline(graphicsDevice, resourceFactory, layouts.ToArray(), shaderSet, MeshBuffer.PrimitiveTopology, FillMode, blendState);
        }

        public Model AddBehavoirs(Func<Model, IBehavior> behaviorResolver)
            => AddBehavoirs(behaviorResolver(this));
        public Model AddBehavoirs(params IBehavior[] behaviors)
        {
            this.behaviors.AddRange(behaviors);
            return this;
        }

        public void Update(GraphicsDevice graphicsDevice, InputHandler inputs, float deltaSeconds)
        {
            if (hasWorldChanged)
            {
                this.graphicsDevice.UpdateBuffer(WorldBuffer, 0, WorldMatrix);
                hasWorldChanged = false;

            }
            if (hasMaterialChanged)
            {
                graphicsDevice.UpdateBuffer(MaterialInfoBuffer, 0, Material.Value);
                hasMaterialChanged = false;
            }

            foreach(var behavior in behaviors)
            {
                // TODO: use current graphic device? or remove param?
                behavior.Update(graphicsSystem, this, deltaSeconds);
            }
        }

        public void Draw(CommandList commandList)
        {
            commandList.SetPipeline(this.pipeline);
            // TODO only set if changed?
            commandList.SetGraphicsResourceSet(0, this.projectionViewWorldResourceSet);
            // TODO: set all ressources to the same buffer/ressource set
            commandList.SetGraphicsResourceSet(1, this.cameraInfoResourceSet);
            commandList.SetGraphicsResourceSet(2, this.lightInfoResourceSet);
            
            commandList.SetGraphicsResourceSet(3, this.materialInfoResourceSet);
            commandList.SetGraphicsResourceSet(4, this.surfaceTextureResourceSet);

            commandList.SetVertexBuffer(0, MeshBuffer.VertexBuffer);
            commandList.SetIndexBuffer(MeshBuffer.IndexBuffer, MeshBuffer.IndexFormat);

            commandList.SetVertexBuffer(1, instanceVertexBuffer);

            commandList.DrawIndexed(
                indexCount: MeshBuffer.IndexLength,
                instanceCount: (uint) instances.Length,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(
                pipeline!.GetHashCode(),
                Vector3.Distance(GetCenter(), cameraPosition),
                this.graphicsSystem.Camera.FarDistance);
        }
    }
    static class GraphicsPipelineFactory
    {
        private static Dictionary<GraphicsPipelineDescription, Pipeline> graphicPipelines = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        public static Pipeline[] GetAll() => graphicPipelines.Values.ToArray();
        public static Pipeline GetGraphicsPipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, ResourceLayout[] resourceLayouts, ShaderSetDescription shaders, PrimitiveTopology primitiveTopology, PolygonFillMode fillMode, BlendStateDescription blendStateDescription)
        {
            var pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = blendStateDescription;

            pipelineDescription.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: fillMode,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = primitiveTopology;
            pipelineDescription.ShaderSet = shaders;

            pipelineDescription.Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription;
            pipelineDescription.ResourceLayouts = resourceLayouts;

            if (graphicPipelines.TryGetValue(pipelineDescription, out var pipeline))
                return pipeline;

            var newPipeline = resourceFactory.CreateGraphicsPipeline(pipelineDescription);
            graphicPipelines.Add(pipelineDescription, newPipeline);
            return newPipeline;            
        }
    }

    static class ResourceLayoutFactory
    {
        private static ResourceLayout? projectionViewWorldLayout;
        public static ResourceLayout GetProjectionViewWorldLayout(ResourceFactory resourceFactory)
        {
            if (projectionViewWorldLayout != null)
                return projectionViewWorldLayout;

            projectionViewWorldLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("World", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            return projectionViewWorldLayout;
        }

        private static ResourceLayout? surfaceTextureLayout;
        public static ResourceLayout GetSurfaceTextureLayout(ResourceFactory resourceFactory)
        {
            if (surfaceTextureLayout != null)
                return surfaceTextureLayout;

            surfaceTextureLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            return surfaceTextureLayout;
        }

        private static ResourceLayout? cameraInfoLayout;
        public static ResourceLayout GetCameraInfoLayout(ResourceFactory resourceFactory)
        {
            if (cameraInfoLayout != null)
                return cameraInfoLayout;

            cameraInfoLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Camera", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            return cameraInfoLayout;
        }

        private static ResourceLayout? lightInfoLayout;
        public static ResourceLayout GetLightInfoLayout(ResourceFactory resourceFactory)
        {
            if (lightInfoLayout != null)
                return lightInfoLayout;

            lightInfoLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Lights", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            return lightInfoLayout;
        }

        private static ResourceLayout? materialInfoLayout;
        public static ResourceLayout GetMaterialInfoLayout(ResourceFactory resourceFactory)
        {
            if (materialInfoLayout != null)
                return materialInfoLayout;

            materialInfoLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Material", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            return materialInfoLayout;
        }
    }
}

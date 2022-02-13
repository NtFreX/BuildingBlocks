using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    //TODO: wrap with model which creates device resources and contains multiple meshes (bone transforms?)
    public class MeshRenderer : CullRenderable, IUpdateable
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly Cached<Vector3> centerCache;

        private BoundingBox boundingBox;
        private bool hasWorldChanged = true;

        public readonly Mutable<bool> IsActive;
        public readonly MeshDeviceBuffer MeshBuffer;
        public readonly IMutable<Transform> Transform;
        public readonly GraphicsSystem GraphicsSystem;

        public string? Name { get; set; }

        public ResourceSet MaterialInfoResourceSet { get; private set; } // TODO: move the resource set to the buffer?
        public ResourceSet SurfaceTextureResourceSet { get; private set; } // TODO: move the resource set to the buffer?
        public ResourceSet ProjectionViewWorldResourceSet { get; private set; }
        public DeviceBuffer WorldBuffer { get; private set; }
        public Matrix4x4 WorldMatrix { get; private set; }

        public override BoundingBox GetBoundingBox() => boundingBox;
        public override Vector3 GetCenter() => centerCache.Get();
        public override RenderPasses RenderPasses => MeshBuffer.Material.Value.Opacity == 1f ? RenderPasses.Standard : RenderPasses.AlphaBlend;

        // TODO: make configurable
        private readonly MeshRenderPass[] meshRenderPasses = MeshRenderPassFactory.RenderPasses.ToArray();

        // give possibilities to customize render passes
        // TODO: do not pass graphics system but mutable camera? or move camera dependency out
        public unsafe MeshRenderer(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem,
            MeshDeviceBuffer meshBuffer, Transform? transform = null, string? name = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.GraphicsSystem = graphicsSystem;

            var realTransform = transform ?? new Transform();

            MeshBuffer = meshBuffer;
            IsActive = new Mutable<bool>(true, this);
            Transform = new Mutable<Transform>(new Transform(realTransform.Position, realTransform.Rotation, realTransform.Scale), this);
            WorldBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            Name = name;

            IsActive.ValueChanged += (_, _) => UpdateShouldRender();
            Transform.ValueChanged += (_, _) => InvalidateWorldCache();
            
            graphicsSystem.Camera.ValueChanged += (_, _) => UpdateProjectionViewWorldResourceSet();

            this.centerCache = new Cached<Vector3>(() => GetBoundingBox().GetCenter());

            this.MeshBuffer.Material.ValueChanged += (_, _) => UpdateShouldRender();
            this.MeshBuffer.TextureView.ValueChanged += (_, _) => UpdateTextureResourceSet();
            this.MeshBuffer.MaterialInfoBuffer.ValueChanged += (_, _) => UpdateMaterialInfoResourceSet();
            this.MeshBuffer.InstanceBoundingBox.ValueChanged += (_, _) => UpdateBoundingBox();

            UpdateMaterialInfoResourceSet();
            UpdateProjectionViewWorldResourceSet();
            UpdateTextureResourceSet();
            InvalidateWorldCache();
            UpdateBoundingBox();
            UpdateShouldRender();
            Update(0f, InputHandler.Empty);
            
        }

        public static unsafe MeshRenderer Create<TShape>(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, MeshDataProvider mesh, TShape shape,
            Transform? transform = null, TextureView ? textureView = null, string? name = null, DeviceBufferPool? deviceBufferPool = null)
                where TShape : unmanaged, IShape
        {
            var data = PhysicsMeshDeviceBuffer<TShape>.Create(graphicsDevice, resourceFactory, mesh, shape, textureView: textureView, deviceBufferPool: deviceBufferPool);
            return new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, data, transform: transform, name: name);
        }

        public static unsafe MeshRenderer Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, 
            MeshDataProvider mesh, Transform? transform = null, TextureView? textureView = null, string? name = null, DeviceBufferPool? deviceBufferPool = null)
        {
            var data = MeshDeviceBuffer.Create(graphicsDevice, resourceFactory, mesh, textureView: textureView, deviceBufferPool: deviceBufferPool);
            return new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, data, transform: transform, name: name);
        }

        private void UpdateMaterialInfoResourceSet()
        {
            this.MaterialInfoResourceSet?.Dispose();

            //TODO: move to mesh buffer?
            var materialLayout = ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory);
            this.MaterialInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(materialLayout, MeshBuffer.MaterialInfoBuffer.Value.RealDeviceBuffer));
        }

        private void UpdateProjectionViewWorldResourceSet()
        {
            this.ProjectionViewWorldResourceSet?.Dispose();

            if (GraphicsSystem.Camera.Value == null)
                return;

            // TODO: move render stages, shader pipe line mapping and ressource layouts out of here? combine with particle renderer?!!!!!!!!!!!
            var projectionViewWorldLayout = ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory);
            this.ProjectionViewWorldResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(projectionViewWorldLayout, GraphicsSystem.Camera.Value.ProjectionBuffer, GraphicsSystem.Camera.Value.ViewBuffer, WorldBuffer));
        }

        private void UpdateTextureResourceSet()
        {
            this.SurfaceTextureResourceSet?.Dispose();

            if (MeshBuffer.TextureView.Value == null)
                return;

            // TODO: support models without texture (render passes?)
            var surfaceTextureLayout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);
            this.SurfaceTextureResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(surfaceTextureLayout, MeshBuffer.TextureView.Value, graphicsDevice.Aniso4xSampler));
        }

        private void UpdateBoundingBox()
        {
            var newBoundingBox = MeshBuffer.InstanceBoundingBox.Value.TransformBoundingBox(Transform.Value);

            Debug.Assert(!newBoundingBox.ContainsNaN(), "The new bounding box should never contain NaN");
            if (boundingBox.ContainsNotOrIsNotSame(newBoundingBox))
            {
                boundingBox = new BoundingBox(newBoundingBox.Min - BoundingBoxSpacer, newBoundingBox.Max + BoundingBoxSpacer);
                centerCache.Invalidate();
                PublishNewBoundingBoxAvailable();
            }
        }

        private void InvalidateWorldCache()
        {
            var newWorldMatrix = Transform.Value.CreateWorldMatrix();

            Debug.Assert(!newWorldMatrix.ContainsNaN(), "The new world matrix should never contain NaN");
            if (newWorldMatrix != WorldMatrix)
            {
                WorldMatrix = newWorldMatrix;
                UpdateBoundingBox();
                hasWorldChanged = true;
            }
        }

        private void UpdateShouldRender()
        {
            var shouldRender = IsActive && MeshBuffer.Material.Value.Opacity > 0f;
            if(this.ShouldRender != shouldRender)
            {
                this.ShouldRender = shouldRender;
            }
        }

        public void Update(float deltaSeconds, InputHandler inputHandler)
        {
            // lazy buffers for draw calls
            if (hasWorldChanged)
            {
                this.graphicsDevice.UpdateBuffer(WorldBuffer, 0, WorldMatrix);
                hasWorldChanged = false;

            }
        }

        public override void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass)
        {
            Debug.Assert(RenderPasses.HasFlag(renderPass));

            // todo group models by mesh render pass (do in graphics system)
            var instanceCount = MeshBuffer.Instances.Value?.Count ?? 1;
            foreach (var meshRednerPass in meshRenderPasses.Where(x => x.CanBindMeshRenderer(this)))
            {
                //todo: seperate in bind model and bind renderpass?
                meshRednerPass.Bind(resourceFactory, this, renderContext, commandList);

                commandList.SetVertexBuffer(0, MeshBuffer.VertexBuffer.Value.RealDeviceBuffer);
                commandList.SetIndexBuffer(MeshBuffer.IndexBuffer.Value.RealDeviceBuffer, MeshBuffer.IndexFormat);
                
                commandList.DrawIndexed(
                    indexCount: MeshBuffer.IndexLength,
                    instanceCount: (uint) instanceCount,
                    indexStart: 0,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return this.GraphicsSystem.Camera.Value == null
                ? new RenderOrderKey() 
                : RenderOrderKey.Create(
                    // TODO: make this based on some better values? resource set ordering?
                    MeshBuffer.Material!.GetHashCode(),
                    Vector3.Distance(GetCenter(), cameraPosition),
                    this.GraphicsSystem.Camera.Value.FarDistance);
        }

        public void Dispose()
        {
            // TODO: implent create and destry graphics resource pattern
            MeshBuffer.Dispose();
            WorldBuffer.Dispose();
        }


        //TOODO: implement pattern
        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, RenderContext rc)
        { }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, RenderContext rc)
        { }

        public override void DestroyDeviceObjects()
        { }
    }

    //TODO: dispose all the stuff below
    static class ResourceSetFactory
    {
        private static Dictionary<ResourceSetDescription, ResourceSet> resourceSets = new Dictionary<ResourceSetDescription, ResourceSet>();

        public static ResourceSet GetResourceSet(ResourceFactory resourceFactory, ResourceSetDescription description)
        {
            if (!resourceSets.TryGetValue(description, out var set))
            {
                set = resourceFactory.CreateResourceSet(ref description);
                resourceSets.Add(description, set);
            }
            return set;
        }
    }
    static class GraphicsPipelineFactory
    {
        private static Dictionary<GraphicsPipelineDescription, Pipeline> graphicPipelines = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        public static Pipeline[] GetAll() => graphicPipelines.Values.ToArray();
        public static Pipeline GetGraphicsPipeline(
            ResourceFactory resourceFactory, 
            ResourceLayout[] resourceLayouts, Framebuffer framebuffer, ShaderSetDescription shaders, 
            PrimitiveTopology primitiveTopology, PolygonFillMode fillMode, 
            BlendStateDescription blendStateDescription, FaceCullMode faceCullMode = FaceCullMode.None)
        {
            var pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = blendStateDescription;

            pipelineDescription.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: faceCullMode,
                fillMode: fillMode,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = primitiveTopology;
            pipelineDescription.ShaderSet = shaders;

            pipelineDescription.Outputs = framebuffer.OutputDescription;
            pipelineDescription.ResourceLayouts = resourceLayouts;

            if (graphicPipelines.TryGetValue(pipelineDescription, out var pipeline))
                return pipeline;

            var newPipeline = resourceFactory.CreateGraphicsPipeline(pipelineDescription);
            graphicPipelines.Add(pipelineDescription, newPipeline);
            return newPipeline;            
        }
    }

    public static class ResourceLayoutFactory
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

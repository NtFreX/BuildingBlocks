using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    //public class ModelLayouts
    //{
    //    public static void Create3d(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, MeshDeviceBuffer meshDeviceBuffer, bool isDebug)
    //    {
    //        var projectionViewWorldLayout = ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory);
    //        var cameraInfoLayout = ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory);
    //        var lightInfoLayout = ResourceLayoutFactory.GetLightInfoLayout(resourceFactory); 
    //        var materialLayout = ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory);
    //        var surfaceTextureLayout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);

    //        var layouts = new[] {
    //            projectionViewWorldLayout,
    //            cameraInfoLayout,
    //            lightInfoLayout,
    //            materialLayout,
    //            surfaceTextureLayout
    //        };

    //        // TODO: factory
    //        var shaders = resourceFactory.CreateFromSpirv(
    //             new ShaderDescription(
    //                ShaderStages.Vertex,
    //                File.ReadAllBytes("resources/shaders/ui.vert"),
    //                "main", isDebug),
    //            new ShaderDescription(
    //                ShaderStages.Fragment,
    //                File.ReadAllBytes("resources/shaders/ui.frag"),
    //                "main", isDebug));

    //        // TODO: cache
    //        var projectionViewWorldResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(projectionViewWorldLayout, graphicsSystem.Camera.ProjectionBuffer, graphicsSystem.Camera.ViewBuffer, WorldBuffer));
    //        var cameraInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(cameraInfoLayout, graphicsSystem.Camera.CameraInfoBuffer));
    //        var lightInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(lightInfoLayout, graphicsSystem.LightSystem.LightBuffer));
    //        var materialInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(materialLayout, MaterialInfoBuffer));
    //        var surfaceTextureResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(surfaceTextureLayout, meshDeviceBuffer.TextureView.Value, graphicsDevice.Aniso4xSampler));

    //        VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(
    //            new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
    //            new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
    //            new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
    //            new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));
    //        vertexLayoutPerInstance.InstanceStepRate = 1;

    //        var shaderSet = new ShaderSetDescription(
    //            vertexLayouts: new VertexLayoutDescription[] { meshDeviceBuffer.VertexLayout, vertexLayoutPerInstance },
    //            shaders: shaders);
    //    }
    //}

    //TODO: rename to mesh renderer?
    public class Model : IRenderable, IDisposable
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly Shader[] shaders;
        private readonly List<IBehavior> behaviors = new List<IBehavior>();
        private readonly Cached<Vector3> centerCache;

        private BoundingBox boundingBox;
        private ResourceSet projectionViewWorldResourceSet;
        private ResourceSet materialInfoResourceSet; // TODO: move the resource set to the buffer?
        private ResourceSet surfaceTextureResourceSet; // TODO: move the resource set to the buffer?
        private Pipeline? pipeline;
        private bool hasWorldChanged = true;
        private bool wasTransparent = false;
        private Vector3 position = Vector3.Zero;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public event EventHandler<MaterialInfo> MaterialChanged;
        public event EventHandler NewBoundingBoxAvailable;

        public readonly Mutable<bool> IsActive;
        public readonly MeshDeviceBuffer MeshBuffer;
        public readonly IMutable<Vector3> Position;
        public readonly IMutable<Quaternion> Rotation;
        public readonly IMutable<Vector3> Scale;
        public readonly GraphicsSystem GraphicsSystem;

        public string? Name { get; set; }

        // TODO: ability to hook up bounding box prediction of moving objects from bepu?

        /// <summary>
        /// If the new calculated bounding box is still within the spacer of the last published bounding box
        /// no NewBoundingBoxAvailable event will be published and the new calculated boundingbox will be discarded
        /// this is to safe performance updating the octree containing all frustum renderables
        /// </summary>
        public Vector3 BoundingBoxSpacer { get; set; } = Vector3.One * 50;

        public DeviceBuffer WorldBuffer { get; private set; }
        public Matrix4x4 WorldMatrix { get; private set; }

        // TODO: move out of meshrenderer and to node or model or component
        public IBehavior[] Behaviors => behaviors.ToArray();
        public BoundingBox GetBoundingBox() => boundingBox;
        public Vector3 GetCenter() => centerCache.Get();

        

        public unsafe Model(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders,
            MeshDeviceBuffer meshBuffer, ModelCreationInfo? creationInfo = null, string? name = null, IBehavior[]? behaviors = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.GraphicsSystem = graphicsSystem;
            this.shaders = shaders;

            if (behaviors != null)
            {
                this.behaviors.AddRange(behaviors);
            }
            
            MeshBuffer = meshBuffer;
            IsActive = new Mutable<bool>(true, this);
            Position = new MutableWrapper<Vector3>(() => this.position, position => this.position = position, this);
            Rotation = new MutableWrapper<Quaternion>(() => this.rotation, rotation => this.rotation = rotation, this);
            Scale = new MutableWrapper<Vector3>(() => this.scale, scale => this.scale = scale, this);
            WorldBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            Name = name;

            Scale.ValueChanged += (_, _) => InvalidateWorldCache();
            Position.ValueChanged += (_, _) => InvalidateWorldCache();
            Rotation.ValueChanged += (_, _) => InvalidateWorldCache();
            
            var realCreationInfo = creationInfo ?? new ModelCreationInfo();
            position = realCreationInfo.Position;
            scale = realCreationInfo.Scale;
            rotation = realCreationInfo.Rotation;
            wasTransparent = this.MeshBuffer.Material.Value.Opacity != 1f;

            graphicsSystem.Camera.ValueChanged += (_, _) => UpdateProjectionViewWorldResourceSet();

            this.centerCache = new Cached<Vector3>(() => GetBoundingBox().GetCenter());

            this.MeshBuffer.TextureView.ValueChanged += (_, _) => UpdateTextureResourceSet();
            this.MeshBuffer.Material.ValueChanged += (_, _) => UpdateMaterialInfo();
            this.MeshBuffer.MaterialInfoBuffer.ValueChanged += (_, _) => UpdateMaterialInfoResourceSet();
            this.MeshBuffer.InstanceBoundingBox.ValueChanged += (_, _) => UpdateBoundingBox();
            this.MeshBuffer.FillMode.ValueChanged += (_, _) => UpdatePipeline();
            this.MeshBuffer.CullMode.ValueChanged += (_, _) => UpdatePipeline();
            this.MeshBuffer.VertexLayout.ValueChanged += (_, _) => UpdatePipeline();
            this.MeshBuffer.PrimitiveTopology.ValueChanged += (_, _) => UpdatePipeline();

            UpdateMaterialInfoResourceSet();
            UpdateProjectionViewWorldResourceSet();
            UpdateTextureResourceSet();
            InvalidateWorldCache();
            UpdateBoundingBox();
            UpdateMaterialInfo();
            UpdatePipeline();
            Update(InputHandler.Empty, 0f);
            
        }

        public static unsafe Model Create<TShape>(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem,
            Shader[] shaders, MeshDataProvider mesh, TShape shape,
            ModelCreationInfo? creationInfo = null, TextureView ? textureView = null, string? name = null, DeviceBufferPool? deviceBufferPool = null)
                where TShape : unmanaged, IShape
        {
            var data = PhysicsMeshDeviceBuffer<TShape>.Create(graphicsDevice, resourceFactory, mesh, shape, textureView: textureView, deviceBufferPool: deviceBufferPool);
            return new Model(graphicsDevice, resourceFactory, graphicsSystem, shaders, data, creationInfo: creationInfo, name: name);
        }

        public static unsafe Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders, 
            MeshDataProvider mesh, ModelCreationInfo? creationInfo = null, TextureView? textureView = null, string? name = null, DeviceBufferPool? deviceBufferPool = null)
        {
            var data = MeshDeviceBuffer.Create(graphicsDevice, resourceFactory, mesh, textureView: textureView, deviceBufferPool: deviceBufferPool);
            return new Model(graphicsDevice, resourceFactory, graphicsSystem, shaders, data, creationInfo: creationInfo, name: name);
        }

        private void UpdateMaterialInfoResourceSet()
        {
            this.materialInfoResourceSet?.Dispose();

            //TODO: move to mesh buffer?
            var materialLayout = ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory);
            this.materialInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(materialLayout, MeshBuffer.MaterialInfoBuffer.Value.RealDeviceBuffer));
        }

        private void UpdateProjectionViewWorldResourceSet()
        {
            this.projectionViewWorldResourceSet?.Dispose();

            if (GraphicsSystem.Camera.Value == null)
                return;

            // TODO: move render stages, shader pipe line mapping and ressource layouts out of here? combine with particle renderer?!!!!!!!!!!!
            var projectionViewWorldLayout = ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory);
            this.projectionViewWorldResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(projectionViewWorldLayout, GraphicsSystem.Camera.Value.ProjectionBuffer, GraphicsSystem.Camera.Value.ViewBuffer, WorldBuffer));
        }


        private void UpdateTextureResourceSet()
        {
            this.surfaceTextureResourceSet?.Dispose();

            // TODO: support models without texture (render passes?)
            var surfaceTextureLayout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);
            this.surfaceTextureResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(surfaceTextureLayout, MeshBuffer.TextureView.Value, graphicsDevice.Aniso4xSampler));
        }

        private void UpdateBoundingBox()
        {
            var newBoundingBox = MeshBuffer.InstanceBoundingBox.Value.TransformBoundingBox(Rotation.Value, Scale.Value, Position.Value);

            Debug.Assert(!newBoundingBox.ContainsNaN(), "The new bounding box should never contain NaN");
            if (boundingBox.ContainsNotOrIsNotSame(newBoundingBox))
            {
                boundingBox = new BoundingBox(newBoundingBox.Min - BoundingBoxSpacer, newBoundingBox.Max + BoundingBoxSpacer);
                centerCache.Invalidate();
                NewBoundingBoxAvailable?.Invoke(this, EventArgs.Empty);
            }
        }

        private void InvalidateWorldCache()
        {
            var newWorldMatrix = Transform.CreateWorldMatrix(Position.Value, Matrix4x4.CreateFromQuaternion(Rotation.Value), Scale.Value);

            Debug.Assert(!newWorldMatrix.ContainsNaN(), "The new world matrix should never contain NaN");
            if (newWorldMatrix != WorldMatrix)
            {
                WorldMatrix = newWorldMatrix;
                UpdateBoundingBox();
                hasWorldChanged = true;
            }
        }

        private void UpdateMaterialInfo()
        {
            if ((MeshBuffer.Material.Value.Opacity == 1f && wasTransparent) || (MeshBuffer.Material.Value.Opacity < 1f && !wasTransparent))
            {
                UpdatePipeline();
            }

            wasTransparent = MeshBuffer.Material.Value.Opacity != 1f;
            MaterialChanged?.Invoke(this, MeshBuffer.Material);
        }

        private void UpdatePipeline()
        {
            var layouts = new List<ResourceLayout>();
            layouts.Add(ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetLightInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory));
            layouts.Add(ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory));

            var blendState = MeshBuffer.Material.Value.Opacity == 1f ? BlendStateDescription.SingleOverrideBlend : BlendStateDescription.SingleAlphaBlend;

            // TODO: do not use in pineline if not used
            VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(
                new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));
            vertexLayoutPerInstance.InstanceStepRate = 1;

            var shaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { MeshBuffer.VertexLayout, vertexLayoutPerInstance },
                shaders: shaders);

            this.pipeline = GraphicsPipelineFactory.GetGraphicsPipeline(graphicsDevice, resourceFactory, layouts.ToArray(), shaderSet, MeshBuffer.PrimitiveTopology, MeshBuffer.FillMode, blendState, MeshBuffer.CullMode);
        }

        public Model AddBehavoirs(params Func<Model, IBehavior>[] behaviorResolvers)
            => AddBehavoirs(behaviorResolvers.Select(resolver => resolver(this)).ToArray());

        public Model AddBehavoirs(params IBehavior[] behaviors)
        {
            this.behaviors.AddRange(behaviors);
            return this;
        }

        public void Update(InputHandler _, float deltaSeconds)
        {
            if (hasWorldChanged)
            {
                this.graphicsDevice.UpdateBuffer(WorldBuffer, 0, WorldMatrix);
                hasWorldChanged = false;

            }

            foreach(var behavior in behaviors)
            {
                // TODO: use current graphic device? or remove param?
                behavior.Update(deltaSeconds);
            }
        }

        public void Draw(CommandList commandList)
        {
            commandList.SetPipeline(this.pipeline);
            // TODO only set if changed?
            commandList.SetGraphicsResourceSet(0, this.projectionViewWorldResourceSet);
            // TODO: set all ressources to the same buffer/ressource set
            commandList.SetGraphicsResourceSet(1, this.GraphicsSystem.Camera.Value!.CameraInfoResourceSet);
            commandList.SetGraphicsResourceSet(2, this.GraphicsSystem.LightSystem.LightInfoResourceSet);
            
            commandList.SetGraphicsResourceSet(3, this.materialInfoResourceSet);
            commandList.SetGraphicsResourceSet(4, this.surfaceTextureResourceSet);

            commandList.SetVertexBuffer(0, MeshBuffer.VertexBuffer.Value.RealDeviceBuffer);
            commandList.SetIndexBuffer(MeshBuffer.IndexBuffer.Value.RealDeviceBuffer, MeshBuffer.IndexFormat);

            commandList.SetVertexBuffer(1, MeshBuffer.InstanceInfoBuffer.Value.RealDeviceBuffer);

            commandList.DrawIndexed(
                indexCount: MeshBuffer.IndexLength,
                instanceCount: (uint) MeshBuffer.Instances.Value.Count,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return this.GraphicsSystem.Camera.Value == null
                ? new RenderOrderKey() 
                : RenderOrderKey.Create(
                    pipeline!.GetHashCode(),
                    Vector3.Distance(GetCenter(), cameraPosition),
                    this.GraphicsSystem.Camera.Value.FarDistance);
        }

        public void Dispose()
        {
            MeshBuffer.Dispose();
            //projectionViewWorldResourceSet.Dispose();
            //materialInfoResourceSet.Dispose();
            WorldBuffer.Dispose();
            //surfaceTextureResourceSet.Dispose();

            //TODO: do not dispose behavoir but deactivate for a model
            foreach(var behavior in behaviors)
            {
                behavior.Dispose();
            }

            ////TODO: is this nessesary? remove or do nicer
            //if (resourceFactory is DisposeCollectorResourceFactory disposeCollectorFactory)
            //{
            //    disposeCollectorFactory.DisposeCollector.Remove(projectionViewWorldResourceSet);
            //    disposeCollectorFactory.DisposeCollector.Remove(cameraInfoResourceSet);
            //    disposeCollectorFactory.DisposeCollector.Remove(lightInfoResourceSet);
            //    disposeCollectorFactory.DisposeCollector.Remove(materialInfoResourceSet);
            //    disposeCollectorFactory.DisposeCollector.Remove(instanceVertexBuffer);
            //    disposeCollectorFactory.DisposeCollector.Remove(WorldBuffer);
            //    disposeCollectorFactory.DisposeCollector.Remove(MaterialInfoBuffer);
            //    disposeCollectorFactory.DisposeCollector.Remove(surfaceTextureResourceSet);
            //}
        }
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
        public static Pipeline GetGraphicsPipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, ResourceLayout[] resourceLayouts, ShaderSetDescription shaders, PrimitiveTopology primitiveTopology, PolygonFillMode fillMode, BlendStateDescription blendStateDescription, FaceCullMode faceCullMode = FaceCullMode.None)
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

            pipelineDescription.Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription;
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

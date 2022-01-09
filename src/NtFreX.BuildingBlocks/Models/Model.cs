using BepuPhysics;
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
        private readonly Collider? collider;
        private readonly ResourceSet projectionViewWorldResourceSet;
        private readonly ResourceSet cameraInfoResourceSet;
        private readonly ResourceSet lightInfoResourceSet;
        private readonly ResourceSet materialInfoResourceSet;
        private readonly ResourceSet surfaceTextureResourceSet;
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Shader[] shaders;
        private readonly VertexLayoutDescription vertexLayout;
        private readonly IndexFormat indexFormat;
        private readonly PrimitiveTopology primitiveTopology;
        private readonly BoundingBox boundingBox;

        public readonly IMutable<Vector3> Position;
        public readonly IMutable<Quaternion> Rotation;
        public readonly IMutable<Vector3> Scale;
        public readonly Mutable<MaterialInfo> Material = new Mutable<MaterialInfo>(new MaterialInfo());
        public readonly Mutable<PolygonFillMode> FillMode = new Mutable<PolygonFillMode>(PolygonFillMode.Solid);

        public string? Name { get; set; }

        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }
        public DeviceBuffer WorldBuffer { get; private set; }
        public DeviceBuffer MaterialInfoBuffer { get; private set; }
        public uint IndexCount { get; private set; }
        public Matrix4x4 WorldMatrix { get; private set; }

        public BoundingBox BoundingBox => BoundingBox.Transform(boundingBox, WorldMatrix);
        public Vector3 Center => BoundingBox.GetCenter();

        private Pipeline? pipeline;
        private bool hasWorldChanged = true;
        private bool hasMaterialChanged = true;
        private bool wasTransparent = false;
        private Vector3 position = Vector3.Zero;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;
        

        public unsafe Model(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, ModelCreationInfo creationInfo, Shader[] shaders, MeshData mesh, VertexLayoutDescription vertexLayout, IndexFormat indexFormat, PrimitiveTopology primitiveTopology, 
                    TextureView? textureView, MaterialInfo? material = null, bool colider = false, bool dynamic = false, float mass = 1f)
        {
            this.collider = colider ? new Collider(primitiveTopology, mesh, simulation, creationInfo, dynamic, mass) : null;
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.graphicsSystem = graphicsSystem;
            this.shaders = shaders;
            this.indexFormat = indexFormat;
            this.primitiveTopology = primitiveTopology;
            this.vertexLayout = vertexLayout;
            
            boundingBox = mesh.GetBoundingBox();
            position = creationInfo.Position;
            scale = creationInfo.Scale;
            rotation = creationInfo.Rotation;
            
            Material.Value = material ?? new MaterialInfo();
            Position = new MutableWrapper<Vector3>(() => GetPose().Position, position => SetPose(position, Rotation?.Value ?? rotation));
            Rotation = new MutableWrapper<Quaternion>(() => GetPose().Orientation, rotation => SetPose(Position?.Value ?? position, rotation));
            Scale = new MutableWrapper<Vector3>(GetScale, SetScale);
            MaterialInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<MaterialInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            WorldBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Scale.ValueChanged += (_, _) => hasWorldChanged = true;
            Position.ValueChanged += (_, _) => hasWorldChanged = true;
            Rotation.ValueChanged += (_, _) => hasWorldChanged = true;
            FillMode.ValueChanged += (_, _) => UpdatePipeline();
            Material.ValueChanged += (_, _) => UpdateMaterialInfo();

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
            this.surfaceTextureResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(surfaceTextureLayout, textureView, graphicsDevice.Aniso4xSampler));

            var commandListDescription = new CommandListDescription();
            var commandList = resourceFactory.CreateCommandList(ref commandListDescription);
            commandList.Begin();

            VertexBuffer = mesh.CreateVertexBuffer(resourceFactory, commandList);
            IndexBuffer = mesh.CreateIndexBuffer(resourceFactory, commandList, out var indexCount);
            IndexCount = (uint)indexCount;

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            commandList.Dispose();

            wasTransparent = Material.Value.Opacity != 1f;

            UpdateMaterialInfo();
            UpdatePipeline();
            Update(graphicsDevice, InputHandler.Empty, 0f);
        }

        private Vector3 GetScale()
        {
            if (collider != null)
            {
                return collider.Scale;
            }
            return this.scale;
        }
        private void SetScale(Vector3 scale)
        {
            if (collider != null)
            {
                collider.Scale = scale;
            }
            this.scale = scale;
        }
        private RigidPose GetPose()
        {
            if (collider != null)
            {
                return collider.GetPose();
            }
            return new RigidPose(position, rotation);
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            if (collider != null)
            {
                collider.SetPose(position, rotation);
            }
            this.position = position;
            this.rotation = rotation;
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

            var shaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: shaders);

            this.pipeline = GraphicsPipelineFactory.GetGraphicsPipeline(graphicsDevice, resourceFactory, layouts.ToArray(), shaderSet, primitiveTopology, FillMode, blendState);
        }

        public void Update(GraphicsDevice graphicsDevice, InputHandler inputs, float deltaSeconds)
        {
            if (collider?.IsDynamic ?? hasWorldChanged)
            {
                WorldMatrix = Matrix4x4.CreateScale(Scale.Value) *
                              Matrix4x4.CreateFromQuaternion(Rotation.Value) *
                              Matrix4x4.CreateTranslation(Position.Value);
                graphicsDevice.UpdateBuffer(WorldBuffer, 0, WorldMatrix);
                hasWorldChanged = false;

            }
            if (hasMaterialChanged)
            {
                graphicsDevice.UpdateBuffer(MaterialInfoBuffer, 0, Material.Value);
                hasMaterialChanged = false;
            }
        }

        public void Draw(CommandList commandList)
        {
            commandList.SetPipeline(this.pipeline);
            // TODO only set if changed?
            commandList.SetGraphicsResourceSet(0, this.projectionViewWorldResourceSet);
            commandList.SetGraphicsResourceSet(1, this.cameraInfoResourceSet);
            commandList.SetGraphicsResourceSet(2, this.lightInfoResourceSet);
            commandList.SetGraphicsResourceSet(3, this.materialInfoResourceSet);
            commandList.SetGraphicsResourceSet(4, this.surfaceTextureResourceSet);

            commandList.SetVertexBuffer(0, VertexBuffer);
            commandList.SetIndexBuffer(IndexBuffer, indexFormat);
            commandList.DrawIndexed(
                indexCount: IndexCount,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(
                pipeline!.GetHashCode(),
                Vector3.Distance(Center, cameraPosition),
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

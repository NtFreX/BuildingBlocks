using NtFreX.BuildingBlocks.Model;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class VertexPositionColorNormalTextureMeshRenderPass : MeshRenderPass
    {
        private readonly Shader[] shaders;
        private readonly ShaderSetDescription shaderSet;
        private readonly ResourceLayout[] resourceLayout;
        private readonly bool requiresSurfaceTexture;
        private readonly bool requiresInstanceBuffer;

        private const int TextureSetIndex = 1;
        private const int WorldViewProjectionSetIndex = 0;

        //TODO: make this nicer?
        public static VertexPositionColorNormalTextureMeshRenderPass[] GetAllCombinations(ResourceFactory resourceFactory, bool isDebug = false)
            => new VertexPositionColorNormalTextureMeshRenderPass[]
            {
                new VertexPositionColorNormalTextureMeshRenderPass(resourceFactory, true, true, isDebug),
                new VertexPositionColorNormalTextureMeshRenderPass(resourceFactory, true, false, isDebug),
                new VertexPositionColorNormalTextureMeshRenderPass(resourceFactory, false, true, isDebug),
                new VertexPositionColorNormalTextureMeshRenderPass(resourceFactory, false, false, isDebug),
            };

        public VertexPositionColorNormalTextureMeshRenderPass(ResourceFactory resourceFactory, bool requiresSurfaceTexture, bool requiresInstanceBuffer, bool isDebug = false)
        {
            var shaderFlags = new Dictionary<string, bool>
            {
                { "hasTexture", requiresSurfaceTexture },
                { "hasInstances", requiresInstanceBuffer }
            };
            var shaderValues = new Dictionary<string, string> 
            { 
                { "textureSet", TextureSetIndex.ToString() },
                { "worldViewProjectionSet", WorldViewProjectionSetIndex.ToString() }
            };

            var resourceLayoutList = new List<ResourceLayout>();
            resourceLayoutList.Add(resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("World", ResourceKind.UniformBuffer, ShaderStages.Vertex))));
            if (requiresSurfaceTexture)
                resourceLayoutList.Add(resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment))));

            var vertexLayoutDescription = new List<VertexLayoutDescription>();
            vertexLayoutDescription.Add(VertexPositionNormalTextureColor.VertexLayout);
            if(requiresInstanceBuffer)
                vertexLayoutDescription.Add(InstanceInfo.VertexLayout);

            // TODO: create device resouces pattern
            shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(resourceFactory, shaderFlags, shaderValues, "Resources/positionNormalTextureColor", isDebug);
            resourceLayout = resourceLayoutList.ToArray();
            shaderSet = new ShaderSetDescription(vertexLayouts: vertexLayoutDescription.ToArray(), shaders: shaders);
            this.requiresSurfaceTexture = requiresSurfaceTexture;
            this.requiresInstanceBuffer = requiresInstanceBuffer;
        }

        protected override Pipeline BuildPipeline(ResourceFactory resourceFactory, RenderContext renderContext, PrimitiveTopology primitiveTopology, PolygonFillMode polygonFillMode, FaceCullMode faceCullMode, BlendStateDescription blendStateDescription)
            => GraphicsPipelineFactory.GetGraphicsPipeline(resourceFactory, resourceLayout, renderContext.MainSceneFramebuffer, shaderSet, primitiveTopology, polygonFillMode, blendStateDescription, faceCullMode);

        protected override void BindResources(MeshRenderer meshRenderer, CommandList commandList)
        {
            commandList.SetGraphicsResourceSet(0, meshRenderer.ProjectionViewWorldResourceSet);
            if (requiresSurfaceTexture)
                commandList.SetGraphicsResourceSet(TextureSetIndex, meshRenderer.SurfaceTextureResourceSet);

            if (requiresInstanceBuffer)
                commandList.SetVertexBuffer(1, meshRenderer.MeshBuffer.InstanceInfoBuffer.Value.RealDeviceBuffer);
        }

        public override void Dispose()
        {
            foreach(var shader in shaders)
            {
                shader.Dispose();
            }
        }

        public override bool CanBindMeshRenderer(MeshRenderer meshRenderer)
        {
            var hasInstances = meshRenderer.MeshBuffer.InstanceInfoBuffer.Value != null && (!meshRenderer.MeshBuffer.Instances.Value?.Equals(InstanceInfo.Single) ?? false);
            var hasSurfaceTexture = meshRenderer.SurfaceTextureResourceSet != null;

            return meshRenderer.MeshBuffer.VertexLayout.Value.Equals(VertexPositionNormalTextureColor.VertexLayout) && this.requiresInstanceBuffer == hasInstances && this.requiresSurfaceTexture == hasSurfaceTexture;
        }
    }
}

using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class DefaultMeshRenderPass : MeshRenderPass
    {
        public class VertexResourceLayoutConfig
        {
            public VertexLayoutDescription VertexLayout;
            public bool IsPositionNormalTextureCoordinateColor;
            public bool IsPositionNormalTextureCoordinate;
            public bool IsPositionNormal;
            public bool IsPosition;

            public bool RequiresBones;
            public bool RequiresSurfaceTexture;
            public bool RequiresInstanceBuffer;
            public bool RequiresAlphaMap;
        }

        private readonly VertexResourceLayoutConfig config;
        private readonly int worldViewProjectionSetIndex;
        private readonly int surfaceTextureSetIndex;
        private readonly int alphaMapTextureSetIndex;
        private readonly int boneTransformationsSetIndex;

        public const int MaxBoneTransforms = 64;

        public static DefaultMeshRenderPass[] GetAllDefaultMeshRenderPassConfigurations(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, bool isDebug = false)
        {
            var vertexLayouts = new List<VertexResourceLayoutConfig>();
            vertexLayouts.Add(new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinateColor = true, VertexLayout = VertexPositionNormalTextureColor.VertexLayout });
            vertexLayouts.Add(new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinate = true, VertexLayout = VertexPositionNormalTexture.VertexLayout });
            vertexLayouts.Add(new VertexResourceLayoutConfig { IsPositionNormal = true, VertexLayout = VertexPositionNormal.VertexLayout });
            vertexLayouts.Add(new VertexResourceLayoutConfig { IsPosition = true, VertexLayout = VertexPosition.VertexLayout });

            return vertexLayouts.SelectMany(layout =>
            {
                var permutations = Permutation.GetAllPermutations(4);
                return permutations.Select(mutation => new VertexResourceLayoutConfig
                {
                    IsPosition = layout.IsPosition,
                    IsPositionNormal = layout.IsPositionNormal,
                    IsPositionNormalTextureCoordinate = layout.IsPositionNormalTextureCoordinate,
                    IsPositionNormalTextureCoordinateColor = layout.IsPositionNormalTextureCoordinateColor,
                    VertexLayout = layout.VertexLayout,
                    RequiresBones = mutation[0],
                    RequiresSurfaceTexture = mutation[1],
                    RequiresInstanceBuffer = mutation[2],
                    RequiresAlphaMap = mutation[3]
                }).ToArray();
            }).Select(config => Create(graphicsDevice, resourceFactory, config, isDebug)).ToArray();
        }

        public DefaultMeshRenderPass(ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts, VertexResourceLayoutConfig config, 
            int worldViewProjectionSetIndex, int surfaceTextureSetIndex, int alphaMapTextureSetIndex, int boneTransformationsSetIndex)
            : base(shaderSetDescription, resourceLayouts)
        {
            this.config = config;
            this.worldViewProjectionSetIndex = worldViewProjectionSetIndex;
            this.surfaceTextureSetIndex = surfaceTextureSetIndex;
            this.alphaMapTextureSetIndex = alphaMapTextureSetIndex;
            this.boneTransformationsSetIndex = boneTransformationsSetIndex;
        }

        public static DefaultMeshRenderPass Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, VertexResourceLayoutConfig config, bool isDebug = false)
        {
            var vertexPositions = config.IsPositionNormalTextureCoordinateColor ? 4 :
                        config.IsPositionNormalTextureCoordinate ? 3 : 
                        config.IsPositionNormal ? 2 :
                        config.IsPosition ? 1 : throw new Exception("Invlaid vertex layout");


            int worldViewProjectionSetIndex = 0;
            int surfaceTextureSetIndex = 1;
            int alphaMapTextureSetIndex = config.RequiresSurfaceTexture ? 2 : 1;
            int boneTransformationsSetIndex = config.RequiresSurfaceTexture && config.RequiresAlphaMap ? 3 :
                                              config.RequiresSurfaceTexture || config.RequiresAlphaMap ? 2 : 1;

            var shaderFlags = new Dictionary<string, bool>
            {
                { "hasTexture", config.RequiresSurfaceTexture },
                { "hasInstances", config.RequiresInstanceBuffer },
                { "hasAlphaMap", config.RequiresAlphaMap },
                { "hasBones", config.RequiresBones },
                { "isPositionNormalTextureCoordinateColor", config.IsPositionNormalTextureCoordinateColor },
                { "isPositionNormalTextureCoordinate", config.IsPositionNormalTextureCoordinate },
                { "isPositionNormal", config.IsPositionNormal },
                { "isPosition", config.IsPosition },
                { "hasColor", config.IsPositionNormalTextureCoordinateColor },
                { "hasTextureCoordinate", config.IsPositionNormalTextureCoordinateColor || config.IsPositionNormalTextureCoordinate }
            };
            var shaderValues = new Dictionary<string, string>
            {
                { "textureSet", surfaceTextureSetIndex.ToString() },
                { "worldViewProjectionSet", worldViewProjectionSetIndex.ToString() },
                { "alphaMapSet", alphaMapTextureSetIndex.ToString() },
                { "boneTransformationsSet", boneTransformationsSetIndex.ToString() },
                { "boneWeightsLocation", vertexPositions.ToString() },
                { "boneIndicesLocation", (vertexPositions + 1).ToString() },
                { "instancePositionLocation", (config.RequiresBones ? vertexPositions + 2 : vertexPositions).ToString() },
                { "instanceRotationLocation", (config.RequiresBones ? vertexPositions + 3 : vertexPositions + 1).ToString() },
                { "instanceScaleLocation", (config.RequiresBones ? vertexPositions + 4 : vertexPositions + 2).ToString() },
                { "instanceTexArrayIndexLocation", (config.RequiresBones ? vertexPositions + 5 : vertexPositions + 3).ToString() },
                { "maxBoneTransforms", MaxBoneTransforms.ToString() },
            };

            var resourceLayoutList = new List<ResourceLayout>();
            resourceLayoutList.Add(ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory));
            if (config.RequiresSurfaceTexture)
                resourceLayoutList.Add(ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory));
            if (config.RequiresAlphaMap)
                resourceLayoutList.Add(ResourceLayoutFactory.GetAlphaMapTextureLayout(resourceFactory));
            if (config.RequiresBones)
                resourceLayoutList.Add(ResourceLayoutFactory.GetBoneTransformationLayout(resourceFactory));

            var vertexLayoutDescription = new List<VertexLayoutDescription>();
            vertexLayoutDescription.Add(config.VertexLayout);
            if (config.RequiresBones)
                vertexLayoutDescription.Add(new VertexLayoutDescription(
                    new VertexElementDescription("BoneWeights", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                    new VertexElementDescription("BoneIndices", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt4)));
            if (config.RequiresInstanceBuffer)
                vertexLayoutDescription.Add(InstanceInfo.VertexLayout);

            // TODO: create device resouces pattern
            var shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, shaderFlags, shaderValues, "Resources/mesh", isDebug);
            var shaderSet = new ShaderSetDescription(vertexLayouts: vertexLayoutDescription.ToArray(), shaders: shaders, ShaderPrecompiler.GetSpecializations(graphicsDevice));

            return new DefaultMeshRenderPass(shaderSet, resourceLayoutList.ToArray(), config, worldViewProjectionSetIndex, surfaceTextureSetIndex, alphaMapTextureSetIndex, boneTransformationsSetIndex);            
        }

        protected override void BindResources(MeshRenderer meshRenderer, CommandList commandList)
        {
            commandList.SetGraphicsResourceSet((uint)worldViewProjectionSetIndex, meshRenderer.ProjectionViewWorldResourceSet);
            if (config.RequiresSurfaceTexture)
                commandList.SetGraphicsResourceSet((uint)surfaceTextureSetIndex, meshRenderer.SurfaceTextureResourceSet);
            if(config.RequiresAlphaMap)
                commandList.SetGraphicsResourceSet((uint)alphaMapTextureSetIndex, meshRenderer.AlphaMapTextureResourceSet);
            if(config.RequiresBones)
                commandList.SetGraphicsResourceSet((uint)boneTransformationsSetIndex, meshRenderer.BonesTransformationsResourceSet);

            commandList.SetVertexBuffer(0, meshRenderer.MeshBuffer.VertexBuffer.Value.RealDeviceBuffer);
            commandList.SetIndexBuffer(meshRenderer.MeshBuffer.IndexBuffer.Value.RealDeviceBuffer, meshRenderer.MeshBuffer.IndexFormat);

            if (config.RequiresBones)
                commandList.SetVertexBuffer(1, meshRenderer.MeshBuffer.BonesInfoBuffer.Value!.RealDeviceBuffer);

            if (config.RequiresInstanceBuffer)
                commandList.SetVertexBuffer((uint) (config.RequiresBones ? 2 : 1), meshRenderer.MeshBuffer.InstanceInfoBuffer.Value!.RealDeviceBuffer);
        }

        public override bool CanBindMeshRenderer(MeshRenderer meshRenderer)
        {
            var hasInstances = meshRenderer.MeshBuffer.InstanceInfoBuffer.Value != null && (!meshRenderer.MeshBuffer.Instances.Value?.Equals(InstanceInfo.Single) ?? false);
            var hasSurfaceTexture = meshRenderer.SurfaceTextureResourceSet != null;
            var hasAlphaMap = meshRenderer.AlphaMapTextureResourceSet != null;
            var hasBones = meshRenderer.BonesTransformationsResourceSet != null && meshRenderer.MeshBuffer.BonesInfoBuffer.Value != null;

            return meshRenderer.MeshBuffer.VertexLayout.Value.Equals(config.VertexLayout) && 
                config.RequiresInstanceBuffer == hasInstances &&
                config.RequiresSurfaceTexture == hasSurfaceTexture &&
                config.RequiresAlphaMap == hasAlphaMap &&
                config.RequiresBones == hasBones;
        }
    }
}

using NtFreX.BuildingBlocks.Light;
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
            public bool RequiresLights;
            public bool RequiresReflection; // TODO: support in mesh buffer
            public bool RequiresMaterial; // TODO: support in mesh buffer
        }

        private readonly VertexResourceLayoutConfig config;
        private readonly int worldViewProjectionSetIndex;
        private readonly int cameraInfoSetIndex;
        private readonly int surfaceTextureSetIndex;
        private readonly int alphaMapTextureSetIndex;
        private readonly int boneTransformationsSetIndex;
        private readonly int reflectionSetIndex;
        private readonly int lightSetIndex;
        private readonly int materialSetIndex;

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
                var permutations = Permutation.GetAllPermutations(6);
                var tasks = permutations.Select((value, index) => (Value: value, Index: index)).GroupBy(value => value.Index % Environment.ProcessorCount).Select(group => Task.Run(() => group.Select(mutation => new VertexResourceLayoutConfig
                {
                    IsPosition = layout.IsPosition,
                    IsPositionNormal = layout.IsPositionNormal,
                    IsPositionNormalTextureCoordinate = layout.IsPositionNormalTextureCoordinate,
                    IsPositionNormalTextureCoordinateColor = layout.IsPositionNormalTextureCoordinateColor,
                    VertexLayout = layout.VertexLayout,
                    RequiresBones = mutation.Value[0],
                    RequiresSurfaceTexture = mutation.Value[1],
                    RequiresInstanceBuffer = mutation.Value[2],
                    RequiresAlphaMap = mutation.Value[3],
                    RequiresLights = mutation.Value[4],
                    RequiresMaterial = mutation.Value[5],
                    RequiresReflection = false // TODO: implement
                }))).ToArray();
                Task.WaitAll(tasks);
                return tasks.SelectMany(task => task.Result).ToArray();
            }).Select(config => Create(graphicsDevice, resourceFactory, config, isDebug)).ToArray();
        }

        public DefaultMeshRenderPass(ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts, VertexResourceLayoutConfig config, 
            int worldViewProjectionSetIndex, int cameraInfoSetIndex, int surfaceTextureSetIndex, int alphaMapTextureSetIndex, int boneTransformationsSetIndex,
            int reflectionSet, int lightSetIndex, int materialSetIndex)
            : base(shaderSetDescription, resourceLayouts)
        {
            this.config = config;
            this.worldViewProjectionSetIndex = worldViewProjectionSetIndex;
            this.cameraInfoSetIndex = cameraInfoSetIndex;
            this.surfaceTextureSetIndex = surfaceTextureSetIndex;
            this.alphaMapTextureSetIndex = alphaMapTextureSetIndex;
            this.boneTransformationsSetIndex = boneTransformationsSetIndex;
            this.reflectionSetIndex = reflectionSet;
            this.lightSetIndex = lightSetIndex;
            this.materialSetIndex = materialSetIndex;
        }

        public static DefaultMeshRenderPass Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, VertexResourceLayoutConfig config, bool isDebug = false)
        {
            var vertexPositions = config.IsPositionNormalTextureCoordinateColor ? 4 :
                        config.IsPositionNormalTextureCoordinate ? 3 : 
                        config.IsPositionNormal ? 2 :
                        config.IsPosition ? 1 : throw new Exception("Invlaid vertex layout");


            var currentSet = 0;
            int worldViewProjectionSetIndex = currentSet++;
            int cameraInfoSetIndex = currentSet++;
            int surfaceTextureSetIndex = config.RequiresSurfaceTexture ? currentSet++ : -1; // TODO: combine those first two with the material layout
            int alphaMapTextureSetIndex = config.RequiresAlphaMap ? currentSet++ : -1;
            int boneTransformationsSetIndex = config.RequiresBones ? currentSet++ : -1;
            int reflectionSetIndex = config.RequiresReflection ? currentSet++ : -1;
            int lightSetIndex = config.RequiresLights ? currentSet++ : -1;
            int materialSetIndex = config.RequiresMaterial ? currentSet++ : -1;

            var shaderFlags = new Dictionary<string, bool>
            {
                { "hasReflection", config.RequiresReflection },
                { "hasLights", config.RequiresLights },
                { "hasMaterial", config.RequiresMaterial },
                { "hasTexture", config.RequiresSurfaceTexture },
                { "hasInstances", config.RequiresInstanceBuffer },
                { "hasAlphaMap", config.RequiresAlphaMap },
                { "hasBones", config.RequiresBones },
                { "isPositionNormalTextureCoordinateColor", config.IsPositionNormalTextureCoordinateColor },
                { "isPositionNormalTextureCoordinate", config.IsPositionNormalTextureCoordinate },
                { "isPositionNormal", config.IsPositionNormal },
                { "isPosition", config.IsPosition },
                { "hasColor", config.IsPositionNormalTextureCoordinateColor },
                { "hasNormal", config.IsPositionNormal || config.IsPositionNormalTextureCoordinate || config.IsPositionNormalTextureCoordinateColor },
                { "hasTextureCoordinate", config.IsPositionNormalTextureCoordinateColor || config.IsPositionNormalTextureCoordinate }
            };
            var shaderValues = new Dictionary<string, string>
            {
                { "textureSet", surfaceTextureSetIndex.ToString() },
                { "worldViewProjectionSet", worldViewProjectionSetIndex.ToString() },
                { "cameraInfoSet", cameraInfoSetIndex.ToString() },
                { "alphaMapSet", alphaMapTextureSetIndex.ToString() },
                { "boneTransformationsSet", boneTransformationsSetIndex.ToString() },
                { "reflectionSet", reflectionSetIndex.ToString() },
                { "lightSet", lightSetIndex.ToString() },
                { "materialSet", materialSetIndex.ToString() },
                { "boneWeightsLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "boneIndicesLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "instancePositionLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceRotationLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceScaleLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceTexArrayIndexLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "maxBoneTransforms", MaxBoneTransforms.ToString() },
                { "maxPointLights", LightInfo.MaxLights.ToString() },
            };

            var resourceLayoutList = new List<ResourceLayout>();
            resourceLayoutList.Add(ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory));
            resourceLayoutList.Add(ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory));
            if (config.RequiresSurfaceTexture)
                resourceLayoutList.Add(ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory));
            if (config.RequiresAlphaMap)
                resourceLayoutList.Add(ResourceLayoutFactory.GetAlphaMapTextureLayout(resourceFactory));
            if (config.RequiresBones)
                resourceLayoutList.Add(ResourceLayoutFactory.GetBoneTransformationLayout(resourceFactory));
            if (config.RequiresReflection)
                throw new NotImplementedException();
            if (config.RequiresMaterial)
                resourceLayoutList.Add(ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory));
            if (config.RequiresLights)
                resourceLayoutList.Add(ResourceLayoutFactory.GetLightInfoLayout(resourceFactory));

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

            return new DefaultMeshRenderPass(shaderSet, resourceLayoutList.ToArray(), config, worldViewProjectionSetIndex, cameraInfoSetIndex, surfaceTextureSetIndex, alphaMapTextureSetIndex, boneTransformationsSetIndex, reflectionSetIndex, lightSetIndex, materialSetIndex);            
        }

        protected override void BindResources(MeshRenderer meshRenderer, CommandList commandList)
        {
            commandList.SetGraphicsResourceSet((uint)worldViewProjectionSetIndex, meshRenderer.ProjectionViewWorldResourceSet);
            commandList.SetGraphicsResourceSet((uint)cameraInfoSetIndex, meshRenderer.GraphicsSystem.Camera.Value?.CameraInfoResourceSet);
            if (config.RequiresSurfaceTexture)
                commandList.SetGraphicsResourceSet((uint)surfaceTextureSetIndex, meshRenderer.SurfaceTextureResourceSet);
            if(config.RequiresAlphaMap)
                commandList.SetGraphicsResourceSet((uint)alphaMapTextureSetIndex, meshRenderer.AlphaMapTextureResourceSet);
            if(config.RequiresBones)
                commandList.SetGraphicsResourceSet((uint)boneTransformationsSetIndex, meshRenderer.BonesTransformationsResourceSet);
            if (config.RequiresReflection)
                throw new NotImplementedException();
            if (config.RequiresMaterial)
                commandList.SetGraphicsResourceSet((uint)materialSetIndex, meshRenderer.MaterialInfoResourceSet);
            if (config.RequiresLights)
                commandList.SetGraphicsResourceSet((uint)lightSetIndex, meshRenderer.GraphicsSystem.LightSystem.LightInfoResourceSet);

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
            var hasLights = meshRenderer.GraphicsSystem.LightSystem != null;
            var hasMaterial = meshRenderer.MaterialInfoResourceSet != null;

            return meshRenderer.MeshBuffer.VertexLayout.Value.Equals(config.VertexLayout) && 
                config.RequiresInstanceBuffer == hasInstances &&
                config.RequiresSurfaceTexture == hasSurfaceTexture &&
                config.RequiresAlphaMap == hasAlphaMap &&
                config.RequiresBones == hasBones && 
                config.RequiresLights == hasLights &&
                config.RequiresMaterial == hasMaterial;
        }
    }
}

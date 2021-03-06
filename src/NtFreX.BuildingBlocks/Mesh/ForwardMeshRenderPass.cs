using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class ForwardMeshRenderPass : MeshRenderPass
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
            public bool RequiresNormalMap;
            public bool RequiresLights;
            public bool RequiresReflection; // TODO: support in mesh buffer
            public bool RequiresMaterial;

            public override int GetHashCode()
                => (VertexLayout, IsPositionNormalTextureCoordinateColor, IsPositionNormalTextureCoordinate, IsPositionNormal, IsPosition, RequiresBones, RequiresSurfaceTexture, RequiresInstanceBuffer, RequiresAlphaMap, RequiresNormalMap, RequiresLights, RequiresReflection, RequiresMaterial).GetHashCode();
        }

        private readonly VertexResourceLayoutConfig config;
        private readonly int viewProjectionSetIndex;
        private readonly int worldSetIndex;
        private readonly int inverseWorldSetIndex;
        private readonly int cameraInfoSetIndex;
        private readonly int surfaceTextureSetIndex;
        private readonly int alphaMapTextureSetIndex;
        private readonly int normalMapTextureSetIndex;
        private readonly int boneTransformationsSetIndex;
        private readonly int reflectionSetIndex;
        private readonly int lightSetIndex;
        private readonly int shadowVertexSetIndex;
        private readonly int shadowFragmentSetIndex;
        private readonly int materialSetIndex;

        public static Task<ForwardMeshRenderPass[]> GetAllConfigurationsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, bool isDebug = false)
        {
            var vertexLayouts = new List<VertexResourceLayoutConfig>
            {
                new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinateColor = true, VertexLayout = VertexPositionNormalTextureColor.VertexLayout },
                new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinate = true, VertexLayout = VertexPositionNormalTexture.VertexLayout },
                new VertexResourceLayoutConfig { IsPositionNormal = true, VertexLayout = VertexPositionNormal.VertexLayout },
                new VertexResourceLayoutConfig { IsPosition = true, VertexLayout = VertexPosition.VertexLayout }
            };

            return Task.WhenAll(vertexLayouts.SelectMany(layout =>
            {
                var permutations = Permutation.GetAllPermutations(7);
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
                    RequiresAlphaMap = mutation[3],
                    RequiresLights = mutation[4],
                    RequiresMaterial = mutation[5],
                    RequiresNormalMap = mutation[6],
                    RequiresReflection = false // TODO: implement
                }).ToArray();
            }).Select(config => Task.Run(() => Create(graphicsDevice, resourceFactory, config, isDebug))));
        }

        public ForwardMeshRenderPass(ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts, VertexResourceLayoutConfig config, 
            int viewProjectionSetIndex, int worldSetIndex, int inverseWorldSetIndex, int cameraInfoSetIndex, int surfaceTextureSetIndex, int alphaMapTextureSetIndex, int normalMapTextureSetIndex, int boneTransformationsSetIndex,
            int reflectionSet, int lightSetIndex, int shadowVertexSetIndex, int shadowFragmentSetIndex, int materialSetIndex)
            : base(shaderSetDescription, resourceLayouts)
        {
            this.config = config;
            this.viewProjectionSetIndex = viewProjectionSetIndex;
            this.worldSetIndex = worldSetIndex;
            this.inverseWorldSetIndex = inverseWorldSetIndex;
            this.cameraInfoSetIndex = cameraInfoSetIndex;
            this.surfaceTextureSetIndex = surfaceTextureSetIndex;
            this.alphaMapTextureSetIndex = alphaMapTextureSetIndex;
            this.normalMapTextureSetIndex = normalMapTextureSetIndex;
            this.boneTransformationsSetIndex = boneTransformationsSetIndex;
            this.reflectionSetIndex = reflectionSet;
            this.lightSetIndex = lightSetIndex;
            this.shadowVertexSetIndex = shadowVertexSetIndex;
            this.shadowFragmentSetIndex = shadowFragmentSetIndex;
            this.materialSetIndex = materialSetIndex;
        }

        public static ForwardMeshRenderPass Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, VertexResourceLayoutConfig config, bool isDebug = false)
        {
            var vertexPositions = config.IsPositionNormalTextureCoordinateColor ? 4 :
                        config.IsPositionNormalTextureCoordinate ? 3 : 
                        config.IsPositionNormal ? 2 :
                        config.IsPosition ? 1 : throw new Exception("Invlaid vertex layout");


            var currentSet = 0;
            int viewProjectionSetIndex = currentSet++;
            int worldSetIndex = currentSet++;
            int inverseWorldSetIndex = currentSet++;
            int cameraInfoSetIndex = currentSet++;
            int surfaceTextureSetIndex = config.RequiresSurfaceTexture ? currentSet++ : -1; // TODO: combine those first two with the material layout
            int alphaMapTextureSetIndex = config.RequiresAlphaMap ? currentSet++ : -1;
            int normalMapTextureSetIndex = config.RequiresNormalMap ? currentSet++ : -1;
            int boneTransformationsSetIndex = config.RequiresBones ? currentSet++ : -1;
            int materialSetIndex = config.RequiresMaterial ? currentSet++ : -1;
            int lightSetIndex = config.RequiresLights ? currentSet++ : -1;
            int shadowVertexSetIndex = config.RequiresLights ? currentSet++ : -1;
            int shadowFragmentSetIndex = config.RequiresLights ? currentSet++ : -1;
            int reflectionSetIndex = config.RequiresReflection ? currentSet++ : -1;

            var shaderFlags = new Dictionary<string, bool>
            {
                { "hasReflection", config.RequiresReflection },
                { "hasLights", config.RequiresLights },
                { "hasMaterial", config.RequiresMaterial },
                { "hasTexture", config.RequiresSurfaceTexture },
                { "hasInstances", config.RequiresInstanceBuffer },
                { "hasAlphaMap", config.RequiresAlphaMap },
                { "hasNormalMap", config.RequiresNormalMap },
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
                { "viewProjectionSet", viewProjectionSetIndex.ToString() },
                { "worldSet", worldSetIndex.ToString() },
                { "inverseWorldSet", inverseWorldSetIndex.ToString() },
                { "cameraInfoSet", cameraInfoSetIndex.ToString() },
                { "alphaMapSet", alphaMapTextureSetIndex.ToString() },
                { "normalMapSet", normalMapTextureSetIndex.ToString() },
                { "boneTransformationsSet", boneTransformationsSetIndex.ToString() },
                { "reflectionSet", reflectionSetIndex.ToString() },
                { "materialSet", materialSetIndex.ToString() },
                { "lightSet", lightSetIndex.ToString() },
                { "shadowVertexSet", shadowVertexSetIndex.ToString() },
                { "shadowFragmentSet", shadowFragmentSetIndex.ToString() },
                { "boneWeightsLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "boneIndicesLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "instancePositionLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceRotationLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceScaleLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceTexArrayIndexLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "maxBoneTransforms", BonesMeshDataSpecialization.MaxBoneTransforms.ToString() },
                { "maxPointLights", PointLightCollectionInfo.MaxLights.ToString() },
            };

            var resourceLayoutList = new List<ResourceLayout>
            {
                ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory),
                ResourceLayoutFactory.GetWorldLayout(resourceFactory),
                ResourceLayoutFactory.GetInverseWorldLayout(resourceFactory),
                ResourceLayoutFactory.GetCameraInfoFragmentLayout(resourceFactory)
            };

            if (config.RequiresSurfaceTexture)
                resourceLayoutList.Add(ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory));
            if (config.RequiresAlphaMap)
                resourceLayoutList.Add(ResourceLayoutFactory.GetAlphaMapTextureLayout(resourceFactory));
            if (config.RequiresNormalMap)
                resourceLayoutList.Add(ResourceLayoutFactory.GetNormalMapTextureLayout(resourceFactory));
            if (config.RequiresBones)
                resourceLayoutList.Add(ResourceLayoutFactory.GetBoneTransformationLayout(resourceFactory));
            if (config.RequiresMaterial)
                resourceLayoutList.Add(ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory));
            if (config.RequiresLights) 
            { 
                resourceLayoutList.Add(ResourceLayoutFactory.GetLightInfoLayout(resourceFactory));
                resourceLayoutList.Add(ResourceLayoutFactory.GetVertexShadowLayout(resourceFactory));
                resourceLayoutList.Add(ResourceLayoutFactory.GetFragmentShadowLayout(resourceFactory));
            }
            if (config.RequiresReflection)
                throw new NotImplementedException();

            var shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, shaderFlags, shaderValues, "Resources/mesh", isDebug);
            var shaderSet = new ShaderSetDescription(vertexLayouts: VertexLayoutFactory.CreateDefaultLayout(config.VertexLayout, config.RequiresBones, config.RequiresInstanceBuffer), shaders: new[] { shaders.VertexShader, shaders.FragementShader }, ShaderPrecompiler.GetSpecializations(graphicsDevice));

            return new ForwardMeshRenderPass(shaderSet, resourceLayoutList.ToArray(), config, viewProjectionSetIndex, worldSetIndex, inverseWorldSetIndex, cameraInfoSetIndex, surfaceTextureSetIndex, alphaMapTextureSetIndex, normalMapTextureSetIndex, boneTransformationsSetIndex, reflectionSetIndex, lightSetIndex, shadowVertexSetIndex, shadowFragmentSetIndex, materialSetIndex);            
        }

        protected override void BindPipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, CommandList commandList)
        {
            Debug.Assert(renderContext.MainSceneFramebuffer != null);

            meshRenderer.MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var phongMaterialMeshDataSpecialization);
            var hasAlphaMap = meshRenderer.MeshData.Specializations.TryGet<AlphaMapMeshDataSpecialization>(out _);

            var blendState = (phongMaterialMeshDataSpecialization == null || phongMaterialMeshDataSpecialization.Material.Value.Opacity == 1f) && !hasAlphaMap ? BlendStateDescription.SingleOverrideBlend : BlendStateDescription.SingleAlphaBlend;
            var pipeLine = GraphicsPipelineFactory.GetGraphicsPipeline(
                graphicsDevice, resourceFactory, ResourceLayouts, renderContext.MainSceneFramebuffer, ShaderSetDescription,
                meshRenderer.MeshData.DrawConfiguration.PrimitiveTopology.Value, meshRenderer.MeshData.DrawConfiguration.FillMode.Value,
                blendState, meshRenderer.MeshData.DrawConfiguration.FaceCullMode.Value);
            pipeLine.Name = nameof(ForwardMeshRenderPass);
            commandList.SetPipeline(pipeLine);
        }

        protected override void BindResources(MeshRenderer meshRenderer, Scene scene, RenderContext renderContext, CommandList commandList)
        {
            Debug.Assert(scene.Camera.Value?.ProjectionViewResourceSet != null);

            commandList.SetGraphicsResourceSet((uint)viewProjectionSetIndex, scene.Camera.Value.ProjectionViewResourceSet);
            commandList.SetGraphicsResourceSet((uint)worldSetIndex, meshRenderer.WorldResourceSet);
            commandList.SetGraphicsResourceSet((uint)inverseWorldSetIndex, meshRenderer.InverseWorldResourceSet);

            Debug.Assert(meshRenderer.CurrentScene?.Camera.Value != null);
            commandList.SetGraphicsResourceSet((uint)cameraInfoSetIndex, meshRenderer.CurrentScene.Camera.Value.CameraInfoResourceSet);

            if (config.RequiresSurfaceTexture)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<SurfaceTextureMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet((uint)surfaceTextureSetIndex, specialization.ResouceSet);
            }

            if (config.RequiresAlphaMap)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<AlphaMapMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet((uint)alphaMapTextureSetIndex, specialization.ResouceSet);
            }

            if (config.RequiresNormalMap)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<NormalMapMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet((uint)normalMapTextureSetIndex, specialization.ResouceSet);
            }

            if (config.RequiresBones)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<BonesMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet((uint)boneTransformationsSetIndex, specialization.ResouceSet);
            }

            if (config.RequiresReflection)
                throw new NotImplementedException();

            if (config.RequiresMaterial) 
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<PhongMaterialMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet((uint)materialSetIndex, specialization.ResouceSet);
            }

            if (config.RequiresLights)
            {
                Debug.Assert(meshRenderer.CurrentScene?.LightSystem.Value != null);

                commandList.SetGraphicsResourceSet((uint)lightSetIndex, meshRenderer.CurrentScene.LightSystem.Value.LightInfoResourceSet);
                commandList.SetGraphicsResourceSet((uint)shadowVertexSetIndex, renderContext.ShadowVertexResourceSet);
                commandList.SetGraphicsResourceSet((uint)shadowFragmentSetIndex, renderContext.ShadowFragmentResourceSet);
            }

            //TODO: do not doublicate this
            Debug.Assert(meshRenderer.VertexBuffer != null);
            commandList.SetVertexBuffer(0, meshRenderer.VertexBuffer.RealDeviceBuffer);

            Debug.Assert(meshRenderer.IndexBuffer != null);
            commandList.SetIndexBuffer(meshRenderer.IndexBuffer.RealDeviceBuffer, meshRenderer.MeshData.DrawConfiguration.IndexFormat);

            if (config.RequiresBones)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<BonesMeshDataSpecialization>();
                Debug.Assert(specialization.BonesBuffer != null);

                commandList.SetVertexBuffer(1, specialization.BonesBuffer.RealDeviceBuffer);
            }

            if (config.RequiresInstanceBuffer)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<InstancedMeshDataSpecialization>();
                Debug.Assert(specialization.InstanceBuffer != null);

                commandList.SetVertexBuffer((uint)(config.RequiresBones ? 2 : 1), specialization.InstanceBuffer.RealDeviceBuffer);
            }
        }

        public override bool CanBindRenderPass(RenderPasses renderPasses)
            => renderPasses.HasFlag(RenderPasses.Forward) || renderPasses.HasFlag(RenderPasses.AlphaBlend);

        public override bool CanBindMeshRenderer(MeshRenderer meshRenderer)
        {
            var hasInstances = meshRenderer.MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancesSpecialization) && instancesSpecialization.InstanceBuffer != null && (!instancesSpecialization.Instances?.Equals(InstanceInfo.Single) ?? false);
            var hasSurfaceTexture = meshRenderer.MeshData.Specializations.TryGet<SurfaceTextureMeshDataSpecialization>(out var surfaceTexture) && surfaceTexture.ResouceSet != null;
            var hasAlphaMap = meshRenderer.MeshData.Specializations.TryGet<AlphaMapMeshDataSpecialization>(out var alphaMap) && alphaMap.ResouceSet != null;
            var hasNormalMap = meshRenderer.MeshData.Specializations.TryGet<NormalMapMeshDataSpecialization>(out var normalMap) && normalMap.ResouceSet != null;
            var hasBones = meshRenderer.MeshData.Specializations.TryGet<BonesMeshDataSpecialization>(out var bonesSpecialization) && bonesSpecialization.ResouceSet != null && bonesSpecialization.BonesBuffer != null;
            var hasLights = meshRenderer.CurrentScene?.LightSystem != null;
            var hasMaterial = meshRenderer.MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var phongMaterial) && phongMaterial.ResouceSet != null;

            return meshRenderer.MeshData.DrawConfiguration.VertexLayout.Equals(config.VertexLayout) && 
                config.RequiresInstanceBuffer == hasInstances &&
                config.RequiresSurfaceTexture == hasSurfaceTexture &&
                config.RequiresAlphaMap == hasAlphaMap &&
                config.RequiresBones == hasBones && 
                config.RequiresLights == hasLights &&
                config.RequiresMaterial == hasMaterial && 
                config.RequiresNormalMap == hasNormalMap;
        }

        public override int GetHashCode()
            => config.GetHashCode();
    }
}

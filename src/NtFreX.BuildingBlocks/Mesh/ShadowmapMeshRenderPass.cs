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
    public class ShadowmapMeshRenderPass : MeshRenderPass
    {
        public class VertexResourceLayoutConfig
        {
            public VertexLayoutDescription VertexLayout;
            public bool IsPositionNormalTextureCoordinateColor;
            public bool IsPositionNormalTextureCoordinate;
            public bool IsPositionNormal;
            public bool IsPosition;

            public bool RequiresBones;
            public bool RequiresInstanceBuffer;

            public override int GetHashCode()
                => (VertexLayout, IsPositionNormalTextureCoordinateColor, IsPositionNormalTextureCoordinate, IsPositionNormal, IsPosition, RequiresBones, RequiresInstanceBuffer).GetHashCode();
        }

        private readonly int shadowmapIndex;
        private readonly VertexResourceLayoutConfig config;

        //TODO; delete this
        public static async Task<ShadowmapMeshRenderPass[]> GetAllConfigurationsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, RenderContext renderContext, bool isDebug = false)
        {
            var vertexLayouts = new List<VertexResourceLayoutConfig>
            {
                new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinateColor = true, VertexLayout = VertexPositionNormalTextureColor.VertexLayout },
                new VertexResourceLayoutConfig { IsPositionNormalTextureCoordinate = true, VertexLayout = VertexPositionNormalTexture.VertexLayout },
                new VertexResourceLayoutConfig { IsPositionNormal = true, VertexLayout = VertexPositionNormal.VertexLayout },
                new VertexResourceLayoutConfig { IsPosition = true, VertexLayout = VertexPosition.VertexLayout }
            };

            return (await Task.WhenAll(vertexLayouts.SelectMany(layout =>
            {
                var permutations = Permutation.GetAllPermutations(2);
                return permutations.Select(mutation => new VertexResourceLayoutConfig
                {
                    IsPosition = layout.IsPosition,
                    IsPositionNormal = layout.IsPositionNormal,
                    IsPositionNormalTextureCoordinate = layout.IsPositionNormalTextureCoordinate,
                    IsPositionNormalTextureCoordinateColor = layout.IsPositionNormalTextureCoordinateColor,
                    VertexLayout = layout.VertexLayout,
                    RequiresBones = mutation[0],
                    RequiresInstanceBuffer = mutation[1],
                }).ToArray();
            }).Select(config => Task.Run(() => Create(graphicsDevice, resourceFactory, config, renderContext, isDebug))))).SelectMany(x => x).ToArray();
        }

        private ShadowmapMeshRenderPass(VertexResourceLayoutConfig config, ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts, int shadowmapIndex)
            : base(shaderSetDescription, resourceLayouts) // TODO: why pass to base class? is this still needed now that the pipeline is created in the sub class?
        {
            this.config = config;
            this.shadowmapIndex = shadowmapIndex;
        }

        public static ShadowmapMeshRenderPass[] Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, VertexResourceLayoutConfig config, RenderContext renderContext, bool isDebug = false)
        {
            var vertexPositions = config.IsPositionNormalTextureCoordinateColor ? 4 :
                        config.IsPositionNormalTextureCoordinate ? 3 :
                        config.IsPositionNormal ? 2 :
                        config.IsPosition ? 1 : throw new Exception("Invlaid vertex layout");
            
            var shaderFlags = new Dictionary<string, bool> {
                { "hasInstances", config.RequiresInstanceBuffer },
                { "hasBones", config.RequiresBones },
                { "isPositionNormalTextureCoordinateColor", config.IsPositionNormalTextureCoordinateColor },
                { "isPositionNormalTextureCoordinate", config.IsPositionNormalTextureCoordinate },
                { "isPositionNormal", config.IsPositionNormal },
                { "isPosition", config.IsPosition },
                { "hasColor", config.IsPositionNormalTextureCoordinateColor },
                { "hasNormal", config.IsPositionNormal || config.IsPositionNormalTextureCoordinate || config.IsPositionNormalTextureCoordinateColor },
                { "hasTextureCoordinate", config.IsPositionNormalTextureCoordinateColor || config.IsPositionNormalTextureCoordinate }
            };
            var shaderVariables = new Dictionary<string, string> { 
                { "viewProjectionSet", "0" },
                { "worldSet", "1" },
                { "boneTransformationsSet", "2" },
                { "boneWeightsLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "boneIndicesLocation", config.RequiresBones ? vertexPositions++.ToString() : "-1" },
                { "instancePositionLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceRotationLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceScaleLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "instanceTexArrayIndexLocation", config.RequiresInstanceBuffer ? vertexPositions++.ToString() : "-1" },
                { "maxBoneTransforms", BonesMeshDataSpecialization.MaxBoneTransforms.ToString() },
            };

            (var depthVS, var depthFS) = ShaderPrecompiler.CompileVertexAndFragmentShaders(graphicsDevice, resourceFactory, shaderFlags, shaderVariables, "Resources/depth", isDebug);
            var shaderDesc = new ShaderSetDescription(
                    VertexLayoutFactory.CreateDefaultLayout(config.VertexLayout, config.RequiresBones, config.RequiresInstanceBuffer),
                    new[] { depthVS, depthFS },
                    new[] { new SpecializationConstant(100, graphicsDevice.IsClipSpaceYInverted) });

            var resourceLayouts = new ResourceLayout[] { ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory), ResourceLayoutFactory.GetWorldLayout(resourceFactory) };

            return new[] { 
                new ShadowmapMeshRenderPass(config, shaderDesc, resourceLayouts, 0),
                new ShadowmapMeshRenderPass(config, shaderDesc, resourceLayouts, 1),
                new ShadowmapMeshRenderPass(config, shaderDesc, resourceLayouts, 2)
            };
        }

        public override bool CanBindMeshRenderer(MeshRenderer meshRenderer)
        {
            var hasInstances = meshRenderer.MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancesSpecialization) && instancesSpecialization.InstanceBuffer != null && (!instancesSpecialization.Instances?.Equals(InstanceInfo.Single) ?? false);
            var hasBones = meshRenderer.MeshData.Specializations.TryGet<BonesMeshDataSpecialization>(out var bonesSpecialization) && bonesSpecialization.ResouceSet != null && bonesSpecialization.BonesBuffer != null;
            return config.RequiresBones == hasBones && config.RequiresInstanceBuffer == hasInstances && meshRenderer.MeshData.DrawConfiguration.VertexLayout.Equals(config.VertexLayout);
        }

        public override bool CanBindRenderPass(RenderPasses renderPasses)
            => renderPasses.HasFlag(RenderPasses.ShadowMapNear) && shadowmapIndex == 0  || renderPasses.HasFlag(RenderPasses.ShadowMapMid) && shadowmapIndex == 1 || renderPasses.HasFlag(RenderPasses.ShadowMapFar) && shadowmapIndex == 2;

        protected override void BindPipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, CommandList commandList)
        {
            var framebuffer = shadowmapIndex == 0 ? renderContext.NearShadowMapFramebuffer :
                              shadowmapIndex == 1 ? renderContext.MidShadowMapFramebuffer :
                              shadowmapIndex == 2 ? renderContext.FarShadowMapFramebuffer : throw new Exception();

            commandList.SetPipeline(GraphicsPipelineFactory.GetGraphicsPipeline(
                graphicsDevice, resourceFactory, ResourceLayouts, framebuffer, ShaderSetDescription, 
                meshRenderer.MeshData.DrawConfiguration.PrimitiveTopology.Value, meshRenderer.MeshData.DrawConfiguration.FillMode.Value,
                BlendStateDescription.Empty, meshRenderer.MeshData.DrawConfiguration.FaceCullMode.Value));
        }

        protected override void BindResources(MeshRenderer meshRenderer, Scene scene, RenderContext renderContext, CommandList commandList)
        {
            Debug.Assert(scene.Camera.Value?.ProjectionViewResourceSet != null);

            var projectionViewSet = shadowmapIndex == 0 ? renderContext.LightProjectionViewSetNear :
                              shadowmapIndex == 1 ? renderContext.LightProjectionViewSetMid :
                              shadowmapIndex == 2 ? renderContext.LightProjectionViewSetFar : throw new Exception();

            commandList.SetGraphicsResourceSet(0, projectionViewSet);
            commandList.SetGraphicsResourceSet(1, meshRenderer.WorldResourceSet);

            if (config.RequiresBones)
            {
                var specialization = meshRenderer.MeshData.Specializations.Get<BonesMeshDataSpecialization>();
                Debug.Assert(specialization.ResouceSet != null);

                commandList.SetGraphicsResourceSet(3, specialization.ResouceSet);
            }

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

        public override int GetHashCode()
            => config.GetHashCode();
    }
}

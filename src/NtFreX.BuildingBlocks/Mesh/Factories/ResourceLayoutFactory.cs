using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Factories
{
    static class ResourceLayoutFactory
    {
        private static ResourceLayout? drawDeltaComputeLayout;
        public static ResourceLayout GetDrawDeltaComputeLayout(ResourceFactory resourceFactory)
        {
            if (drawDeltaComputeLayout != null)
                return drawDeltaComputeLayout;

            drawDeltaComputeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("DrawDelta", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            drawDeltaComputeLayout.Name = "drawDeltaComputeLayout";

            return drawDeltaComputeLayout;
        }

        private static ResourceLayout? particleResetLayout;
        public static ResourceLayout GetParticleResetLayout(ResourceFactory resourceFactory)
        {
            if (particleResetLayout != null)
                return particleResetLayout;

            particleResetLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ParticleReset", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            particleResetLayout.Name = "particleResetLayout";

            return particleResetLayout;
        }

        private static ResourceLayout? particleBoundsLayout;
        public static ResourceLayout GetParticleBoundsLayout(ResourceFactory resourceFactory)
        {
            if (particleBoundsLayout != null)
                return particleBoundsLayout;

            particleBoundsLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ParticleBounds", ResourceKind.UniformBuffer, ShaderStages.Compute)));
            particleBoundsLayout.Name = "particleBoundsLayout";

            return particleBoundsLayout;
        }

        private static ResourceLayout? worldLayout;
        public static ResourceLayout GetWorldLayout(ResourceFactory resourceFactory)
        {
            if (worldLayout != null)
                return worldLayout;

            worldLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("World", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            worldLayout.Name = "worldLayout";

            return worldLayout;
        }

        private static ResourceLayout? inverseWorldLayout;
        public static ResourceLayout GetInverseWorldLayout(ResourceFactory resourceFactory)
        {
            if (inverseWorldLayout != null)
                return inverseWorldLayout;

            inverseWorldLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InverseWorld", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            inverseWorldLayout.Name = "inverseWorldLayout";

            return inverseWorldLayout;
        }

        private static ResourceLayout? projectionViewLayout;
        public static ResourceLayout GetProjectionViewLayout(ResourceFactory resourceFactory)
        {
            if (projectionViewLayout != null)
                return projectionViewLayout;

            projectionViewLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            projectionViewLayout.Name = "projectionViewLayout";

            return projectionViewLayout;
        }

        private static ResourceLayout? shadowVertexLayout;
        public static ResourceLayout GetVertexShadowLayout(ResourceFactory resourceFactory)
        {
            if (shadowVertexLayout != null)
                return shadowVertexLayout;

            shadowVertexLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("LightNearProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightNearView", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightMidProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightMidView", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightFarProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightFarView", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            shadowVertexLayout.Name = "shadowVertexLayout";
            
            return shadowVertexLayout;
        }

        private static ResourceLayout? shadowFragmentLayout;
        public static ResourceLayout GetFragmentShadowLayout(ResourceFactory resourceFactory)
        {
            if (shadowFragmentLayout != null)
                return shadowFragmentLayout;

            shadowFragmentLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CascadeLimits", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapNear", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapMid", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapFar", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            shadowFragmentLayout.Name = "shadowFragmentLayout";

            return shadowFragmentLayout;
        }

        private static ResourceLayout? surfaceTextureLayout;
        public static ResourceLayout GetSurfaceTextureLayout(ResourceFactory resourceFactory)
        {
            if (surfaceTextureLayout != null)
                return surfaceTextureLayout;

            surfaceTextureLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            surfaceTextureLayout.Name = "surfaceTextureLayout";

            return surfaceTextureLayout;
        }

        private static ResourceLayout? normalMapTextureLayout;
        public static ResourceLayout GetNormalMapTextureLayout(ResourceFactory resourceFactory)
        {
            if (normalMapTextureLayout != null)
                return normalMapTextureLayout;

            normalMapTextureLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("AlphaMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            normalMapTextureLayout.Name = "normalMapTextureLayout";

            return normalMapTextureLayout;
        }

        private static ResourceLayout? alphaMapTextureLayout;
        public static ResourceLayout GetAlphaMapTextureLayout(ResourceFactory resourceFactory)
        {
            if (alphaMapTextureLayout != null)
                return alphaMapTextureLayout;

            alphaMapTextureLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("AlphaMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            alphaMapTextureLayout.Name = "alphaMapTextureLayout";

            return alphaMapTextureLayout;
        }

        private static ResourceLayout? boneTransformationLayout;
        public static ResourceLayout GetBoneTransformationLayout(ResourceFactory resourceFactory)
        {
            if (boneTransformationLayout != null)
                return boneTransformationLayout;

            boneTransformationLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Bones", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            boneTransformationLayout.Name = "boneTransformationLayout";

            return boneTransformationLayout;
        }
        
        private static ResourceLayout? cameraInfoFragmentLayout;
        public static ResourceLayout GetCameraInfoFragmentLayout(ResourceFactory resourceFactory)
        {
            if (cameraInfoFragmentLayout != null)
                return cameraInfoFragmentLayout;

            cameraInfoFragmentLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Camera", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            cameraInfoFragmentLayout.Name = "cameraInfoFragmentLayout";

            return cameraInfoFragmentLayout;
        }

        private static ResourceLayout? cameraInfoVertexLayout;
        public static ResourceLayout GetCameraInfoVertexLayout(ResourceFactory resourceFactory)
        {
            if (cameraInfoVertexLayout != null)
                return cameraInfoVertexLayout;

            cameraInfoVertexLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Camera", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            cameraInfoVertexLayout.Name = "cameraInfoVertexLayout";
            return cameraInfoVertexLayout;
        }

        private static ResourceLayout? lightInfoLayout;
        public static ResourceLayout GetLightInfoLayout(ResourceFactory resourceFactory)
        {
            if (lightInfoLayout != null)
                return lightInfoLayout;

            lightInfoLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("DirectionalLights", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("PointLights", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            lightInfoLayout.Name = "lightInfoLayout";

            return lightInfoLayout;
        }

        private static ResourceLayout? materialInfoLayout;
        public static ResourceLayout GetMaterialInfoLayout(ResourceFactory resourceFactory)
        {
            if (materialInfoLayout != null)
                return materialInfoLayout;

            materialInfoLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("Material", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            materialInfoLayout.Name = "materialInfoLayout";

            return materialInfoLayout;
        }

        public static void Dispose()
        {
            worldLayout?.Dispose();
            worldLayout = null;
            projectionViewLayout?.Dispose();
            projectionViewLayout = null;
            surfaceTextureLayout?.Dispose();
            surfaceTextureLayout = null;
            alphaMapTextureLayout?.Dispose();
            alphaMapTextureLayout = null;
            boneTransformationLayout?.Dispose();
            boneTransformationLayout = null;
            cameraInfoFragmentLayout?.Dispose();
            cameraInfoFragmentLayout = null;
            cameraInfoVertexLayout?.Dispose();
            cameraInfoVertexLayout = null;
            inverseWorldLayout?.Dispose();
            inverseWorldLayout = null;
            particleBoundsLayout?.Dispose();
            particleBoundsLayout = null;
            lightInfoLayout?.Dispose();
            lightInfoLayout = null;
            materialInfoLayout?.Dispose();
            materialInfoLayout = null;
        }
    }
}

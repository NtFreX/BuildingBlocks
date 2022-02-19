using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{

    //        var layouts = new ResourceLayout[] {
    //            ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory),
    //            ResourceLayoutFactory.GetCameraInfoLayout(resourceFactory),
    //            ResourceLayoutFactory.GetLightInfoLayout(resourceFactory),
    //            ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory),
    //            ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory)
    //        };

    //        // TODO: do not use instanced in pineline if not used
    //        var shaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { VertexPositionNormalTextureColor.VertexLayout, InstanceInfo.VertexLayout }, shaders: shaders);

    //        return GraphicsPipelineFactory.GetGraphicsPipeline(resourceFactory, layouts.ToArray(), renderContext.MainSceneFramebuffer, shaderSet, primitiveTopology, polygonFillMode, blendStateDescription, faceCullMode);

    //    protected override void BindResources(MeshRenderer meshRenderer, CommandList commandList)
    //    {
    //        commandList.SetGraphicsResourceSet(0, meshRenderer.ProjectionViewWorldResourceSet);
    //        // TODO: set all ressources to the same buffer/ressource set
    //        commandList.SetGraphicsResourceSet(1, meshRenderer.GraphicsSystem.Camera.Value!.CameraInfoResourceSet);
    //        commandList.SetGraphicsResourceSet(2, meshRenderer.GraphicsSystem.LightSystem.LightInfoResourceSet);

    //        commandList.SetGraphicsResourceSet(3, meshRenderer.MaterialInfoResourceSet);
    //        commandList.SetGraphicsResourceSet(4, meshRenderer.SurfaceTextureResourceSet);
    //    }
    //}

    public static class MeshRenderPassFactory
    {

        public static readonly List<MeshRenderPass> RenderPasses = new List<MeshRenderPass>();

        public static void Load(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, bool isDebug)
        {
            // TODO: make nicer?
            RenderPasses.AddRange(DefaultMeshRenderPass.GetAllDefaultMeshRenderPassConfigurations(graphicsDevice, resourceFactory, isDebug));
        }

        public static void Unload()
        {
            foreach(var value in RenderPasses)
            {
                value.Dispose();
            }
            RenderPasses.Clear();
        }
    }
}

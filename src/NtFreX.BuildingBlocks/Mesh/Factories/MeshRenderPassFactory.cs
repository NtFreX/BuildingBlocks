using NtFreX.BuildingBlocks.Model;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Factories
{
    public static class MeshRenderPassFactory
    {

        public static readonly List<MeshRenderPass> RenderPasses = new ();

        public static async Task LoadAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, RenderContext renderContext, bool isDebug)
        {
            // TODO: make nicer?
            RenderPasses.AddRange(await ForwardMeshRenderPass.GetAllConfigurationsAsync(graphicsDevice, resourceFactory, isDebug));
            RenderPasses.AddRange(await GeometryMeshRenderPass.GetAllConfigurationsAsync(graphicsDevice, resourceFactory, isDebug));
            RenderPasses.AddRange(await ShadowmapMeshRenderPass.GetAllConfigurationsAsync(graphicsDevice, resourceFactory, renderContext, isDebug));
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

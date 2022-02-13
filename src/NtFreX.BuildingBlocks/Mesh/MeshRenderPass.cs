using NtFreX.BuildingBlocks.Model;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public abstract class MeshRenderPass : IDisposable
    {
        protected abstract Pipeline BuildPipeline(ResourceFactory resourceFactory, RenderContext renderContext, PrimitiveTopology primitiveTopology, PolygonFillMode polygonFillMode, FaceCullMode faceCullMode, BlendStateDescription blendStateDescription);
        protected abstract void BindResources(MeshRenderer meshRenderer, CommandList commandList);
        public abstract bool CanBindMeshRenderer(MeshRenderer meshRenderer);

        public void Bind(ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, CommandList commandList)
        {
            var blendState = meshRenderer.MeshBuffer.Material.Value.Opacity == 1f ? BlendStateDescription.SingleOverrideBlend : BlendStateDescription.SingleAlphaBlend;
            commandList.SetPipeline(BuildPipeline(resourceFactory, renderContext, meshRenderer.MeshBuffer.PrimitiveTopology.Value, meshRenderer.MeshBuffer.FillMode.Value, meshRenderer.MeshBuffer.CullMode.Value, blendState));
            BindResources(meshRenderer, commandList);
        }

        public abstract void Dispose();
    }
}

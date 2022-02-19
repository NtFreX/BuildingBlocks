using NtFreX.BuildingBlocks.Model;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public abstract class MeshRenderPass : IDisposable
    {
        protected ShaderSetDescription ShaderSetDescription { get; }
        protected ResourceLayout[] ResourceLayouts { get; }

        protected MeshRenderPass(ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts)
        {
            ShaderSetDescription = shaderSetDescription;
            ResourceLayouts = resourceLayouts;
        }


        protected abstract void BindResources(MeshRenderer meshRenderer, CommandList commandList);
        public abstract bool CanBindMeshRenderer(MeshRenderer meshRenderer);
        
        public virtual void Draw(MeshRenderer meshRenderer, CommandList commandList)
        {
            var instanceCount = meshRenderer.MeshBuffer.Instances.Value?.Count ?? 1;
            commandList.DrawIndexed(
                indexCount: meshRenderer.MeshBuffer.IndexLength,
                instanceCount: (uint)instanceCount,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public void Bind(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, CommandList commandList)
        {
            var blendState = (meshRenderer.MeshBuffer.Material.Value == null || meshRenderer.MeshBuffer.Material.Value.Value.Opacity == 1f) && meshRenderer.AlphaMapTextureResourceSet == null ? BlendStateDescription.SingleOverrideBlend : BlendStateDescription.SingleAlphaBlend;
            // TODO: order by pipeline and then by resource sets?
            var pipeLine = GraphicsPipelineFactory.GetGraphicsPipeline(graphicsDevice, resourceFactory, ResourceLayouts, renderContext.MainSceneFramebuffer, ShaderSetDescription, meshRenderer.MeshBuffer.PrimitiveTopology.Value, meshRenderer.MeshBuffer.FillMode.Value, blendState, meshRenderer.MeshBuffer.CullMode.Value);
            commandList.SetPipeline(pipeLine);
            BindResources(meshRenderer, commandList);
        }

        public void Dispose()
        {
            foreach(var shader in ShaderSetDescription.Shaders)
            {
                shader.Dispose();
            }
        }
    }
}

using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Model;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public abstract class MeshRenderPass : IDisposable
{
    protected ShaderSetDescription ShaderSetDescription { get; }
    protected ResourceLayout[] ResourceLayouts { get; }

    protected MeshRenderPass(ShaderSetDescription shaderSetDescription, ResourceLayout[] resourceLayouts)
    {
        ShaderSetDescription = shaderSetDescription;
        ResourceLayouts = resourceLayouts;
    }

    protected abstract void BindPipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, CommandList commandList);
    protected abstract void BindResources(MeshRenderer meshRenderer, Scene scene, RenderContext renderContext, CommandList commandList);
    public abstract bool CanBindMeshRenderer(MeshRenderer meshRenderer);
    public abstract bool CanBindRenderPass(RenderPasses renderPasses);

    public virtual void Draw(MeshRenderer meshRenderer, CommandList commandList)
    {
        meshRenderer.MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancedMeshDataSpecialization);

        var instanceCount = instancedMeshDataSpecialization?.Instances.Value.Length ?? 1;
        commandList.DrawIndexed(
            indexCount: meshRenderer.IndexCount,
            instanceCount: (uint)instanceCount,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);
    }

    public void Bind(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshRenderer meshRenderer, RenderContext renderContext, Scene scene, CommandList commandList)
    {
        BindPipeline(graphicsDevice, resourceFactory, meshRenderer, renderContext, commandList);
        BindResources(meshRenderer, scene, renderContext, commandList);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach(var shader in ShaderSetDescription.Shaders)
        {
            shader.Dispose();
        }
    }
}

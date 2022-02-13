using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using Veldrid;

using BepuBufferPool = BepuUtilities.Memory.BufferPool;


namespace NtFreX.BuildingBlocks.Mesh.Import;

// TODO: support 16bit import
// TODO: support different vertext layouts?
public abstract class ModelImporter
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly ResourceFactory resourceFactory;
    private readonly TextureFactory textureFactory;
    private readonly GraphicsSystem graphicsSystem;

    public ModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
    {
        this.graphicsDevice = graphicsDevice;
        this.resourceFactory = resourceFactory;
        this.textureFactory = textureFactory;
        this.graphicsSystem = graphicsSystem;
    }

    public abstract Task<MeshDataProvider<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath);

    public Task<MeshRenderer[]> ModelFromFileAsync(string filePath, BepuBufferPool? physicsBufferPool = null, string? name = null)
        => ModelFromFileAsync(new Transform(), filePath, physicsBufferPool, name);

    public async Task<MeshRenderer[]> ModelFromFileAsync(Transform transform, string filePath, BepuBufferPool? physicsBufferPool = null, string? name = null)
    {
        var directory = Path.GetDirectoryName(filePath);
        var meshesh = await PositionColorNormalTexture32BitMeshFromFileAsync(filePath);
        return await Task.WhenAll(meshesh.Select(async mesh =>
        {
            TextureView? texture = null;
            if (!string.IsNullOrEmpty(mesh.TexturePath))
            {
                var path = string.IsNullOrEmpty(directory) ? mesh.TexturePath : Path.Combine(directory, mesh.TexturePath);
                texture = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
            }
            else
            {
                // TODO: remove this line
                texture = textureFactory.GetEmptyTexture(TextureUsage.Sampled);
            }

            if (physicsBufferPool == null)
            {
                return MeshRenderer.Create(
                            graphicsDevice, resourceFactory, graphicsSystem, mesh,
                            transform: transform, textureView: texture, name: name);
            }

            return MeshRenderer.Create(
                graphicsDevice, resourceFactory, graphicsSystem, 
                mesh, mesh.GetPhysicsMesh(physicsBufferPool, transform.Scale), 
                transform: transform, textureView: texture, name: name);
        }));
    }
}
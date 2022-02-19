using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;

using BepuBufferPool = BepuUtilities.Memory.BufferPool;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public record ModelLoadOptions
{
    public Transform Transform = new Transform();
    public BepuBufferPool? PhysicsBufferPool = null;
    public DeviceBufferPool? DeviceBufferPool = null;
    public string? Name = null;
}

public class ImportedMeshCollection<TMeshDataProvider>
    where TMeshDataProvider : BaseMeshDataProvider
{
    public TMeshDataProvider[] Meshes { get; set; } = Array.Empty<TMeshDataProvider>();
    public MeshTransform[] Instaces { get; set; } = Array.Empty<MeshTransform>();
}

public static class ImportedMeshCollectionExtensions
{
    //TODO: possibility to create models as instanced data
    public static MeshDataProvider<VertexPositionNormalTextureColor, Index32>[] FlattenMeshDataProviders(this MeshDataProvider<VertexPositionNormalTextureColor, Index32>[] meshes, MeshTransform[] instances)
    {
        return instances.Select(instance => 
            meshes[instance.MeshIndex].MutateVertices(vertex => 
                new VertexPositionNormalTextureColor(Vector3.Transform(vertex.Position, instance.Transform.CreateWorldMatrix()), vertex.Color, vertex.TextureCoordinate, vertex.Normal))).ToArray();
    }
}

public record MeshTransform
{
    public uint MeshIndex;
    public Transform Transform;
    public string? SurfaceTexture;
    public MaterialInfo? Material;
}

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

    public abstract Task<ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath);

    public async Task<MeshRenderer[]> ModelFromFileAsync(string filePath, ModelLoadOptions? modelLoadOptions = null)
    {
        modelLoadOptions = modelLoadOptions ?? new ModelLoadOptions();

        var directory = Path.GetDirectoryName(filePath);
        var collection = await PositionColorNormalTexture32BitMeshFromFileAsync(filePath);
        return await Task.WhenAll(collection.Instaces.Select(async meshInstance =>
        {
            var mesh = collection.Meshes[meshInstance.MeshIndex];
            var transform = modelLoadOptions.Transform * meshInstance.Transform;

            //TODO: delete this line
            //mesh.Vertices = mesh.Vertices.Select(vertex => new VertexPositionNormalTextureColor(vertex.Position, RgbaFloat.LightGrey, vertex.TextureCoordinate, vertex.Normal)).ToArray();

            // TODO: move texture and material etc out of mesh provider/buffer? (this will overwrite the last material..)
            if(meshInstance.Material != null)
            {
                mesh.Material = meshInstance.Material;
            }

            TextureView? texture = null;
            if(!string.IsNullOrEmpty(meshInstance.SurfaceTexture))
            {
                var path = string.IsNullOrEmpty(directory) ? mesh.TexturePath : Path.Combine(directory, meshInstance.SurfaceTexture);
                texture = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(mesh.TexturePath))
            {
                var path = string.IsNullOrEmpty(directory) ? mesh.TexturePath : Path.Combine(directory, mesh.TexturePath);
                texture = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
            }

            TextureView? alphaMap = null;
            if (!string.IsNullOrEmpty(mesh.AlphaMapPath))
            {
                var path = string.IsNullOrEmpty(directory) ? mesh.AlphaMapPath : Path.Combine(directory, mesh.AlphaMapPath);
                alphaMap = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
            }

            MeshRenderer renderer;
            if (modelLoadOptions.PhysicsBufferPool == null)
            {
                renderer = MeshRenderer.Create(
                            graphicsDevice, resourceFactory, graphicsSystem, mesh,
                            transform: transform, textureView: texture, alphaMap, name: modelLoadOptions.Name, deviceBufferPool: modelLoadOptions.DeviceBufferPool);
            }
            else
            {
                renderer = MeshRenderer.Create(
                    graphicsDevice, resourceFactory, graphicsSystem,
                    mesh, mesh.GetPhysicsMesh(modelLoadOptions.PhysicsBufferPool, transform.Scale),
                    transform: transform, textureView: texture, alphaMap, name: modelLoadOptions.Name, deviceBufferPool: modelLoadOptions.DeviceBufferPool);
            }

            //renderer.MeshBuffer.Material.Value = meshInstance.Material;
            return renderer;
        }));
    }
}
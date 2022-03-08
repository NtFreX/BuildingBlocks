using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;

using BepuBufferPool = BepuUtilities.Memory.BufferPool;
using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;


namespace NtFreX.BuildingBlocks.Mesh.Import;

public record ModelLoadOptions
{
    public Transform Transform = new ();
    public BepuBufferPool? PhysicsBufferPool = null;
    public DeviceBufferPool? DeviceBufferPool = null;
    public string? Name = null;
    public bool IsActive = true;
}

// TODO: support 16bit import
// TODO: support different vertext layouts?
public abstract class ModelImporter
{
    protected readonly TextureFactory TextureFactory;

    public ModelImporter(TextureFactory textureFactory)
    {
        TextureFactory = textureFactory;
    }

    public abstract Task<DefinedMeshData<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath, DeviceBufferPool? deviceBufferPool = null);

    public async Task<MeshRenderer[]> ModelFromFileAsync(string filePath, ModelLoadOptions? modelLoadOptions = null)
    {
        modelLoadOptions ??= new ModelLoadOptions();
        
        var directory = Path.GetDirectoryName(filePath);
        var collection = await PositionColorNormalTexture32BitMeshFromFileAsync(filePath, modelLoadOptions.DeviceBufferPool);
        return await Task.WhenAll(collection.Select(async mesh =>
        {
            if (modelLoadOptions.PhysicsBufferPool != null)
            {
                mesh.Specializations.AddOrUpdate(new BepuPhysicsShapeMeshDataSpecialization<BepuPhysicsMesh>(mesh.GetPhysicsMesh(modelLoadOptions.PhysicsBufferPool, modelLoadOptions.Transform.Scale)));
            }

            // TODO: support to load file from disk at later time
            return await MeshRenderer.CreateAsync(new StaticMeshDataProvider(mesh), modelLoadOptions.Transform, modelLoadOptions.Name, modelLoadOptions.IsActive);
        }));
    }
}
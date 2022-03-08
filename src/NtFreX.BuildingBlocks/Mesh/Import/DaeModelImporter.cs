using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class DaeModelImporter : ModelImporter
{
    public DaeModelImporter(TextureFactory textureFactory)
        : base(textureFactory) { }

    public override Task<DefinedMeshData<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath, DeviceBufferPool? deviceBufferPool = null)
        => DaeFileReader.MeshesFromFileAsync(TextureFactory, filePath, deviceBufferPool);
}

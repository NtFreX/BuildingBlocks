using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Texture;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class DaeModelImporter : ModelImporter
{
    public DaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override async Task<ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var collection = await DaeFileReader.BinaryMeshFromFileAsync(filePath);
        var mutatedMeshes = collection.Meshes.Select(provider => provider.Define<VertexPositionNormalTextureColor, Index32>(data => VertexPositionNormalTextureColor.Build(data, provider.VertexLayout))).ToArray();
        return new ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>
        {
            Instaces = collection.Instaces,
            Meshes = mutatedMeshes
        };
    }
}

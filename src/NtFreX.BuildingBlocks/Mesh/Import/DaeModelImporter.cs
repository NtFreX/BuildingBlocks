using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class DaeModelImporter : ModelImporter
{
    public DaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override async Task<MeshDataProvider<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var meshProviders = await DaeFileReader.BinaryMeshFromFileAsync(filePath);
        return meshProviders.Select(provider => provider
            .Define<VertexPositionNormalTextureColor, Index32>(data => VertexPositionNormalTextureColor.Build(data, provider.VertexLayout))
            .MutateVertices(vertex => new VertexPositionNormalTextureColor(Vector3.Transform(vertex.Position, provider.Transform), vertex.Color, vertex.TextureCoordinate, vertex.Normal))).ToArray();
    }
}

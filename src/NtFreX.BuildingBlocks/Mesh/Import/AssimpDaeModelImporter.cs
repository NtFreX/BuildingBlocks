using Assimp;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class AssimpDaeModelImporter : ModelImporter
{
    public AssimpDaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override Task<MeshDataProvider<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var assimpContext = new AssimpContext();
        using (var stream = File.OpenRead(filePath))
        {
            var scene = assimpContext.ImportFileFromStream(stream, Path.GetExtension(filePath));
            var meshes = new List<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>();
            for (var meshIndex = 0; meshIndex < scene.Meshes.Count; meshIndex++)
            {
                var mesh = scene.Meshes[meshIndex];
                    var type = mesh.PrimitiveType == PrimitiveType.Point ? PrimitiveTopology.PointList :
                            mesh.PrimitiveType == PrimitiveType.Line ? PrimitiveTopology.LineList :
                            mesh.PrimitiveType == PrimitiveType.Triangle ? PrimitiveTopology.TriangleList :
                            throw new ArgumentException();

                var shaderReadyVertices = new List<VertexPositionNormalTextureColor>();
                for (var i = 0; i < mesh.VertexCount; i++)
                {
                    var position = mesh.HasVertices ? new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z) : Vector3.Zero;
                    var normal = Vector3.Zero;
                    if (mesh.HasNormals)
                    {
                        normal = new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z);
                    }
                    var textureCordinate = Vector2.Zero;
                    if (mesh.HasTextureCoords(0))
                    {
                        var assimpTextCord = mesh.TextureCoordinateChannels[0][i];
                        textureCordinate = new Vector2(assimpTextCord.X, assimpTextCord.Y);
                    }
                    var color = new RgbaFloat();
                    if (mesh.HasVertexColors(0))
                    {
                        var assimpColor = mesh.VertexColorChannels[0][i];
                        color = new RgbaFloat(assimpColor.R, assimpColor.G, assimpColor.B, assimpColor.A);
                    }

                    shaderReadyVertices.Add(new VertexPositionNormalTextureColor(position, color, textureCordinate, normal));
                }

                //TODO: load all textures            
                //TODO: load material file?
                var meshMaterial = scene.Materials[mesh.MaterialIndex];
                string? texture = meshMaterial.HasTextureDiffuse ? meshMaterial.TextureDiffuse.FilePath : null;

                var material = new MaterialInfo(
                    ambientColor: meshMaterial.ColorAmbient.ToSystemVector(),
                    diffuseColor: meshMaterial.ColorDiffuse.ToSystemVector(),
                    emissiveColor: meshMaterial.ColorEmissive.ToSystemVector(),
                    specularColor: meshMaterial.ColorSpecular.ToSystemVector(),
                    reflectiveColor: meshMaterial.ColorReflective.ToSystemVector(),
                    transparentColor: meshMaterial.ColorTransparent.ToSystemVector(),
                    opacity: meshMaterial.Opacity,
                    reflectivity: meshMaterial.Reflectivity,
                    shininess: meshMaterial.Shininess,
                    shininessStrength: meshMaterial.ShininessStrength);

                var vertices = shaderReadyVertices.ToArray();
                var indices = mesh.GetUnsignedIndices().Select(x => (Index32)x).ToArray();
                meshes.Add(new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(vertices, indices, type, material: material, texturePath: texture));
            }

            var results = new List<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>();
            Transform(results, meshes.ToArray(), scene.RootNode);

            return Task.FromResult(results.ToArray());
        }
    }

    private void Transform(List<MeshDataProvider<VertexPositionNormalTextureColor, Index32>> results, MeshDataProvider<VertexPositionNormalTextureColor, Index32>[] meshes, Node node)
    {
        foreach (var mesh in node.MeshIndices)
        {
            var transform = node.Transform.ToNumericsMatrix();
            results.Add(meshes[mesh].MutateVertices(vertex => new VertexPositionNormalTextureColor(Vector3.Transform(vertex.Position, transform), vertex.Color, vertex.TextureCoordinate, vertex.Normal)));
        }
        foreach (var child in node.Children)
        {
            Transform(results, meshes, child);
        }
    }
}
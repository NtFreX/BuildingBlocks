using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Texture;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class ObjModelImporter : ModelImporter
{
    public ObjModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override Task<ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        var parser = new ObjParser();
        var importCollection = new ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>();
        using (var stream = File.OpenRead(filePath)) 
        {
            var scene = parser.Parse(stream);

            var materialParser = new MtlParser();
            using (var materialStream = File.OpenRead(string.IsNullOrEmpty(directory) ? scene.MaterialLibName : Path.Combine(directory, scene.MaterialLibName)))
            {
                var material = materialParser.Parse(materialStream);

                var meshes = new List<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>();
                foreach (var group in scene.MeshGroups)
                {
                    var materialDef = material.Definitions[group.Material];
                        
                    var fileMesh = scene.GetData(group, new RgbaFloat(0, 0, 0, 1));

                    var materialInfo = new MaterialInfo(
                        opacity: materialDef.Opacity,
                        shininessStrength: (materialDef.SpecularReflectivity.X + materialDef.SpecularReflectivity.Y + materialDef.SpecularReflectivity.Z) / 3f,
                        shininess: materialDef.SpecularExponent);

                    meshes.Add(new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(
                        fileMesh.Vertices,
                        fileMesh.Indices,
                        PrimitiveTopology.TriangleList,
                        materialName: group.Material,
                        texturePath: !string.IsNullOrEmpty(materialDef.DiffuseTexture) ? materialDef.DiffuseTexture : null,
                        alphaMapPath: !string.IsNullOrEmpty(materialDef.AlphaMap) ? materialDef.AlphaMap : null,
                        material: materialInfo));
                }

                importCollection.Instaces = meshes.Select((mesh, index) => new MeshTransform() { MeshIndex = (uint) index, Transform = new () }).ToArray();
                importCollection.Meshes = meshes.ToArray();
                return Task.FromResult(importCollection);
            }
        }
    }
}

using NtFreX.BuildingBlocks.Texture;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class ObjModelImporter : ModelImporter
{
    public ObjModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override Task<MeshDataProvider<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        var parser = new ObjParser();
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
                        
                    var fileMesh = scene.GetData(group, new RgbaFloat(materialDef.DiffuseReflectivity.X, materialDef.DiffuseReflectivity.Y, materialDef.DiffuseReflectivity.Z, 1f));

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
                        material: materialInfo));
                }
                return Task.FromResult(meshes.ToArray());
            }
        }
    }
}

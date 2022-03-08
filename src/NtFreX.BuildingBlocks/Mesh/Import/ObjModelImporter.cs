using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class ObjModelImporter : ModelImporter
{
    public ObjModelImporter(TextureFactory textureFactory)
        : base(textureFactory) { }

    public override Task<DefinedMeshData<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath, DeviceBufferPool? deviceBufferPool = null)
    {
        var parser = new ObjParser();
        var materialParser = new MtlParser();
        var directory = Path.GetDirectoryName(filePath);

        using var stream = File.OpenRead(filePath);
        var scene = parser.Parse(stream);

        using var materialStream = File.OpenRead(string.IsNullOrEmpty(directory) ? scene.MaterialLibName : Path.Combine(directory, scene.MaterialLibName));
        var material = materialParser.Parse(materialStream);

        var meshes = new List<DefinedMeshData<VertexPositionNormalTextureColor, Index32>>();
        foreach (var group in scene.MeshGroups)
        {
            var materialDef = material.Definitions[group.Material];
                        
            var (vertices, indices) = scene.GetData(group, new RgbaFloat(0, 0, 0, 1));
            var specializations = new MeshDataSpecializationDictionary();

            var materialInfo = new PhongMaterialInfo(
                opacity: materialDef.Opacity,
                shininessStrength: (materialDef.SpecularReflectivity.X + materialDef.SpecularReflectivity.Y + materialDef.SpecularReflectivity.Z) / 3f,
                shininess: materialDef.SpecularExponent);

            specializations.AddOrUpdate(new PhongMaterialMeshDataSpecialization(materialInfo, group.Material, deviceBufferPool));
                    
            if (!string.IsNullOrEmpty(materialDef.DiffuseTexture))
            {
                var path = Path.IsPathRooted(materialDef.DiffuseTexture) || string.IsNullOrEmpty(directory) ? materialDef.DiffuseTexture : Path.Combine(directory, materialDef.DiffuseTexture);
                specializations.AddOrUpdate(new SurfaceTextureMeshDataSpecialization(new DirectoryTextureProvider(TextureFactory, path)));
            }

            if (!string.IsNullOrEmpty(materialDef.AlphaMap))
            {
                var path = Path.IsPathRooted(materialDef.AlphaMap) || string.IsNullOrEmpty(directory) ? materialDef.AlphaMap : Path.Combine(directory, materialDef.AlphaMap);
                specializations.AddOrUpdate(new AlphaMapMeshDataSpecialization(new DirectoryTextureProvider(TextureFactory, path)));
            }

            meshes.Add(new DefinedMeshData<VertexPositionNormalTextureColor, Index32>(
                vertices,
                indices,
                PrimitiveTopology.TriangleList,
                meshDataSpecializations: specializations));
        }

        return Task.FromResult(meshes.ToArray());
    }
}

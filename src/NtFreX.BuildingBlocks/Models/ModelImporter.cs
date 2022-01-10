using Assimp;
using BepuPhysics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public abstract class ModelImporter
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly TextureFactory textureFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Simulation simulation;

        public ModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem, Simulation simulation)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.textureFactory = textureFactory;
            this.graphicsSystem = graphicsSystem;
            this.simulation = simulation;
        }

        public abstract Task<MeshDataProvider[]> MeshFromFileAsync(string filePath);

        public async Task<Model[]> ModelFromFileAsync(ModelCreationInfo creationInfo, Shader[] shaders, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var meshesh = await MeshFromFileAsync(filePath);
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
                    texture = await textureFactory.GetEmptyTextureAsync(TextureUsage.Sampled).ConfigureAwait(false);
                }

                return Model.Create(
                            graphicsDevice, resourceFactory, graphicsSystem, simulation, creationInfo, shaders,
                            mesh, VertexPositionColorNormalTexture.VertexLayout, mesh.IndexFormat,
                            mesh.PrimitiveTopology, textureView: texture, material: mesh.Material);
            }));
        }
    }

    public class DaeModelImporter : ModelImporter
    {
        public DaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem, Simulation simulation)
            : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem, simulation) { }

        public override Task<MeshDataProvider[]> MeshFromFileAsync(string filePath)
        {
            AssimpContext assimpContext = new AssimpContext();
            using (var stream = File.OpenRead(filePath))
            {
                Scene scene = assimpContext.ImportFileFromStream(stream, Path.GetExtension(filePath));
                var meshes = new List<MeshDataProvider>();
                foreach (var mesh in scene.Meshes)
                {
                    var type = mesh.PrimitiveType == PrimitiveType.Point ? PrimitiveTopology.PointList :
                                mesh.PrimitiveType == PrimitiveType.Line ? PrimitiveTopology.LineList :
                                mesh.PrimitiveType == PrimitiveType.Triangle ? PrimitiveTopology.TriangleList :
                                throw new ArgumentException();

                    var shaderReadyVertices = new List<VertexPositionColorNormalTexture>();
                    for (var i = 0; i < mesh.VertexCount; i++)
                    {
                        var position = mesh.HasVertices ? new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z) : Vector3.Zero;
                        var normal = Vector3.Zero;
                        if (mesh.HasNormals)
                        {
                            normal = new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z);
                        }
                        else if (mesh.HasVertices)
                        {
                            normal = Vector3.Normalize(new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));
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

                        shaderReadyVertices.Add(new VertexPositionColorNormalTexture(position, color, textureCordinate, normal));
                    }

                    //TODO: load all textures            
                    //TODO: load material file?
                    var meshMaterial = scene.Materials[mesh.MaterialIndex];
                    string? texture = meshMaterial.HasTextureDiffuse ? meshMaterial.TextureDiffuse.FilePath : null;

                    var material = new MaterialInfo
                    {
                        AmbientColor = meshMaterial.ColorAmbient.ToSystemVector(),
                        DiffuseColor = meshMaterial.ColorDiffuse.ToSystemVector(),
                        EmissiveColor = meshMaterial.ColorEmissive.ToSystemVector(),
                        SpecularColor = meshMaterial.ColorSpecular.ToSystemVector(),
                        ReflectiveColor = meshMaterial.ColorReflective.ToSystemVector(),
                        TransparentColor = meshMaterial.ColorTransparent.ToSystemVector(),
                        Opacity = meshMaterial.Opacity,
                        Reflectivity = meshMaterial.Reflectivity,
                        Shininess = meshMaterial.Shininess,
                        ShininessStrength = meshMaterial.ShininessStrength,
                    };

                    var vertices = shaderReadyVertices.ToArray();
                    var indices = mesh.GetUnsignedIndices();
                    meshes.Add(new MeshDataProvider<VertexPositionColorNormalTexture, uint>(
                        vertices, indices, IndexFormat.UInt32, type,
                        VertexPositionColorNormalTexture.VertexLayout,
                        material: material, bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition, 
                        texturePath: texture));
                }
                return Task.FromResult(meshes.ToArray());
            }
        }
    }

    public class ObjModelImporter : ModelImporter
    {

        public ObjModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem, Simulation simulation)
            : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem, simulation) { }

        public override Task<MeshDataProvider[]> MeshFromFileAsync(string filePath)
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

                    var meshes = new List<MeshDataProvider>();
                    foreach (ObjFile.MeshGroup group in scene.MeshGroups)
                    {
                        var fileMesh = scene.GetMesh(group);

                        var materialDef = material.Definitions[fileMesh.MaterialName];
                        var materialInfo = new MaterialInfo
                        {
                            Opacity = materialDef.Opacity,
                            ShininessStrength = (materialDef.SpecularReflectivity.X + materialDef.SpecularReflectivity.Y + materialDef.SpecularReflectivity.Z) / 3f,
                            Shininess = materialDef.SpecularExponent
                        };

                        meshes.Add(new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(
                            fileMesh.Vertices.Select(x => new VertexPositionColorNormalTexture(x)).ToArray(),
                            fileMesh.GetIndices(),
                            IndexFormat.UInt16, PrimitiveTopology.TriangleList,
                            VertexPositionColorNormalTexture.VertexLayout,
                            materialName: fileMesh.MaterialName,
                            bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition,
                            texturePath: string.IsNullOrEmpty(materialDef.DiffuseTexture) ? materialDef.DiffuseTexture : null,
                            material: materialInfo));
                    }
                    return Task.FromResult(meshes.ToArray());
                }
            }
        }
    }
}

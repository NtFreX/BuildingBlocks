using Assimp;
using BepuPhysics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    interface IModelImporter
    {
        Task<Model[]> FromFileAsync(ModelCreationInfo creationInfo, Shader[] shaders, string filePath);
    }

    public class DaeModelImporter : IModelImporter
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly TextureFactory textureFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Simulation simulation;

        public DaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem, Simulation simulation)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.textureFactory = textureFactory;
            this.graphicsSystem = graphicsSystem;
            this.simulation = simulation;
        }

        public async Task<Model[]> FromFileAsync(ModelCreationInfo creationInfo, Shader[] shaders, string filePath)
        {
            AssimpContext assimpContext = new AssimpContext();
            using (var stream = File.OpenRead(filePath))
            {
                Scene scene = assimpContext.ImportFileFromStream(stream, Path.GetExtension(filePath));
                var meshes = new List<Model>();
                var directory = Path.GetDirectoryName(filePath);
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
                    TextureView? diffuseTexture = null;
                    var meshMaterial = scene.Materials[mesh.MaterialIndex];
                    if (meshMaterial.HasTextureDiffuse)
                    {
                        var path = string.IsNullOrEmpty(directory) ? meshMaterial.TextureDiffuse.FilePath : Path.Combine(directory, meshMaterial.TextureDiffuse.FilePath);
                        diffuseTexture = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
                    }
                    else
                    {
                        diffuseTexture = await textureFactory.GetEmptyTextureAsync(TextureUsage.Sampled).ConfigureAwait(false);
                    }

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
                    var meshData = new MeshDataProvider<VertexPositionColorNormalTexture, uint>(vertices, indices, vertex => vertex.Position, IndexFormat.UInt16);
                    meshes.Add(new Model(
                        graphicsDevice, resourceFactory, graphicsSystem, simulation, creationInfo, shaders,
                        meshData, VertexPositionColorNormalTexture.VertexLayout, IndexFormat.UInt32,
                        type, diffuseTexture, material)
                    {
                        Name = mesh.Name
                    });
                }
                return meshes.ToArray();
            }
        }
    }

    public class ObjModelImporter : IModelImporter
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly TextureFactory textureFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Simulation simulation;

        public ObjModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem, Simulation simulation)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.textureFactory = textureFactory;
            this.graphicsSystem = graphicsSystem;
            this.simulation = simulation;
        }

        public async Task<Model[]> FromFileAsync(ModelCreationInfo creationInfo, Shader[] shaders, string filePath)
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

                    var meshes = new List<Model>();
                    foreach (ObjFile.MeshGroup group in scene.MeshGroups)
                    {
                        var fileMesh = scene.GetMesh(group);
                        var mesh = new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(
                            fileMesh.Vertices.Select(x => new VertexPositionColorNormalTexture(x)).ToArray(),
                            fileMesh.GetIndices(),
                            vertex => vertex.Position,
                            IndexFormat.UInt16, fileMesh.MaterialName);

                        var indices = mesh.GetIndices();

                        var materialDef = material.Definitions[mesh.MaterialName];
                        var materialInfo = new MaterialInfo
                        {
                            Opacity = materialDef.Opacity,
                            ShininessStrength = (materialDef.SpecularReflectivity.X + materialDef.SpecularReflectivity.Y + materialDef.SpecularReflectivity.Z) / 3f,
                            Shininess = materialDef.SpecularExponent
                        };

                        TextureView? diffuseTexture = null;
                        if (!string.IsNullOrEmpty(materialDef.DiffuseTexture))
                        {
                            var path = string.IsNullOrEmpty(directory) ? materialDef.DiffuseTexture : Path.Combine(directory, materialDef.DiffuseTexture);
                            diffuseTexture = await textureFactory.GetTextureAsync(path, TextureUsage.Sampled).ConfigureAwait(false);
                        }
                        else
                        {
                            diffuseTexture = await textureFactory.GetEmptyTextureAsync(TextureUsage.Sampled).ConfigureAwait(false);
                        }

                        meshes.Add(new Model(
                            graphicsDevice, resourceFactory, graphicsSystem, simulation, creationInfo, shaders,
                            mesh, VertexPositionColorNormalTexture.VertexLayout, IndexFormat.UInt16,
                            PrimitiveTopology.TriangleList, diffuseTexture, materialInfo));
                    }
                    return meshes.ToArray();
                }
            }
        }
    }
}

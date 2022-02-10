using Assimp;
using BepuPhysics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;
using static Veldrid.Utilities.ObjFile;
using BepuBufferPool = BepuUtilities.Memory.BufferPool;

namespace NtFreX.BuildingBlocks.Mesh
{
    // TODO: support 16bit import
    // TODO: support different vertext layouts?
    public abstract class ModelImporter
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly TextureFactory textureFactory;
        private readonly GraphicsSystem graphicsSystem;

        public ModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.textureFactory = textureFactory;
            this.graphicsSystem = graphicsSystem;
        }

        public abstract Task<MeshDataProvider<VertexPositionColorNormalTexture, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath);

        public Task<Model[]> ModelFromFileAsync(Shader[] shaders, string filePath, BepuBufferPool? physicsBufferPool = null, string? name = null)
            => ModelFromFileAsync(new ModelCreationInfo(), shaders, filePath, physicsBufferPool, name);

        public async Task<Model[]> ModelFromFileAsync(ModelCreationInfo creationInfo, Shader[] shaders, string filePath, BepuBufferPool? physicsBufferPool = null, string? name = null)
        {
            var directory = Path.GetDirectoryName(filePath);
            var meshesh = await PositionColorNormalTexture32BitMeshFromFileAsync(filePath);
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
                    texture = textureFactory.GetEmptyTexture(TextureUsage.Sampled);
                }

                if (physicsBufferPool == null)
                {
                    return Model.Create(
                                graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh,
                                creationInfo: creationInfo, textureView: texture, name: name);
                }

                return Model.Create(
                    graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh, mesh.GetPhysicsMesh(physicsBufferPool, creationInfo.Scale),
                    creationInfo: creationInfo, textureView: texture, name: name);
            }));
        }
    }


    public class DaeModelImporter : ModelImporter
    {
        public DaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
            : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

        public override async Task<MeshDataProvider<VertexPositionColorNormalTexture, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
        {
            var meshProviders = await DaeFileReader.BinaryMeshFromFileAsync(filePath);
            return meshProviders.Select(provider => provider
                .Define<VertexPositionColorNormalTexture, Index32>(data => VertexPositionColorNormalTexture.Build(data, provider.VertexLayout))
                .MutateVertices(vertex => new VertexPositionColorNormalTexture(Vector3.Transform(vertex.Position, provider.Transform), vertex.Color, vertex.TextureCoordinate, vertex.Normal))).ToArray();
        }
    }

    public class AssimpDaeModelImporter : ModelImporter
    {
        public AssimpDaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
            : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

        public override Task<MeshDataProvider<VertexPositionColorNormalTexture, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
        {
            AssimpContext assimpContext = new AssimpContext();
            using (var stream = File.OpenRead(filePath))
            {
                Scene scene = assimpContext.ImportFileFromStream(stream, Path.GetExtension(filePath));
                var meshes = new List<MeshDataProvider<VertexPositionColorNormalTexture, Index32>>();
                for (var meshIndex = 0; meshIndex < scene.Meshes.Count; meshIndex++)
                {
                    var mesh = scene.Meshes[meshIndex];
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
                    meshes.Add(new MeshDataProvider<VertexPositionColorNormalTexture, Index32>(vertices, indices, type, material: material, texturePath: texture));
                }

                var results = new List<MeshDataProvider<VertexPositionColorNormalTexture, Index32>>();
                Transform(results, meshes.ToArray(), scene.RootNode);

                return Task.FromResult(results.ToArray());
            }
        }

        private void Transform(List<MeshDataProvider<VertexPositionColorNormalTexture, Index32>> results, MeshDataProvider<VertexPositionColorNormalTexture, Index32>[] meshes, Node node)
        {
            foreach (var mesh in node.MeshIndices)
            {
                var transform = node.Transform.ToNumericsMatrix();
                results.Add(meshes[mesh].MutateVertices(vertex => new VertexPositionColorNormalTexture(Vector3.Transform(vertex.Position, transform), vertex.Color, vertex.TextureCoordinate, vertex.Normal)));
            }
            foreach (var child in node.Children)
            {
                Transform(results, meshes, child);
            }
        }
    }

    public class ObjModelImporter : ModelImporter
    {
        public ObjModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
            : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

        public override Task<MeshDataProvider<VertexPositionColorNormalTexture, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
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

                    var meshes = new List<MeshDataProvider<VertexPositionColorNormalTexture, Index32>>();
                    foreach (var group in scene.MeshGroups)
                    {
                        var materialDef = material.Definitions[group.Material];
                        
                        var fileMesh = scene.GetData(group, new RgbaFloat(materialDef.DiffuseReflectivity.X, materialDef.DiffuseReflectivity.Y, materialDef.DiffuseReflectivity.Z, 1f));

                        var materialInfo = new MaterialInfo(
                            opacity: materialDef.Opacity,
                            shininessStrength: (materialDef.SpecularReflectivity.X + materialDef.SpecularReflectivity.Y + materialDef.SpecularReflectivity.Z) / 3f,
                            shininess: materialDef.SpecularExponent);

                        meshes.Add(new MeshDataProvider<VertexPositionColorNormalTexture, Index32>(
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

    public static class ObjFileExtensions
    {
        public static (VertexPositionColorNormalTexture[] Vertices, Index32[] Indices) GetData(this ObjFile objFile, MeshGroup group, RgbaFloat color)
        {
            var vertexMap = new Dictionary<FaceVertex, uint>();
            var indices = new Index32[group.Faces.Length * 3];
            var vertices = new List<VertexPositionColorNormalTexture>();

            for (int i = 0; i < group.Faces.Length; i++)
            {
                var face = group.Faces[i];
                uint index0 = GetOrCreate(objFile, vertexMap, vertices, face.Vertex0, face.Vertex1, face.Vertex2, color);
                uint index1 = GetOrCreate(objFile, vertexMap, vertices, face.Vertex1, face.Vertex2, face.Vertex0, color);
                uint index2 = GetOrCreate(objFile, vertexMap, vertices, face.Vertex2, face.Vertex0, face.Vertex1, color);

                // Reverse winding order here.
                indices[(i * 3)] = index0;
                indices[(i * 3) + 2] = index1;
                indices[(i * 3) + 1] = index2;
            }

            return (vertices.ToArray(), indices);
        }

        private static uint GetOrCreate(
            ObjFile objFile,
            Dictionary<FaceVertex, uint> vertexMap,
            List<VertexPositionColorNormalTexture> vertices,
            FaceVertex key,
            FaceVertex adjacent1,
            FaceVertex adjacent2,
            RgbaFloat color)
        {
            uint index;
            if (!vertexMap.TryGetValue(key, out index))
            {
                var vertex = ConstructVertex(objFile, key, adjacent1, adjacent2, color);
                vertices.Add(vertex);
                index = checked((uint)(vertices.Count - 1));
                vertexMap.Add(key, index);
            }

            return index;
        }

        private static VertexPositionColorNormalTexture ConstructVertex(ObjFile objFile, FaceVertex key, FaceVertex adjacent1, FaceVertex adjacent2, RgbaFloat color)
        {
            Vector3 position = objFile.Positions[key.PositionIndex - 1];
            Vector3 normal;
            if (key.NormalIndex == -1)
            {
                normal = ComputeNormal(objFile, key, adjacent1, adjacent2);
            }
            else
            {
                normal = objFile.Normals[key.NormalIndex - 1];
            }


            Vector2 texCoord = key.TexCoordIndex == -1 ? Vector2.Zero : objFile.TexCoords[key.TexCoordIndex - 1];

            return new VertexPositionColorNormalTexture(position, color, texCoord, normal);
        }

        private static Vector3 ComputeNormal(ObjFile objFile, FaceVertex v1, FaceVertex v2, FaceVertex v3)
        {
            Vector3 pos1 = objFile.Positions[v1.PositionIndex - 1];
            Vector3 pos2 = objFile.Positions[v2.PositionIndex - 1];
            Vector3 pos3 = objFile.Positions[v3.PositionIndex - 1];

            return Vector3.Normalize(Vector3.Cross(pos1 - pos2, pos1 - pos3));
        }
    }
}

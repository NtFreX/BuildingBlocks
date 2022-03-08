using Assimp;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Standard.Pools;

using Matrix4x4 = System.Numerics.Matrix4x4;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using AssimpScene = Assimp.Scene;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class AssimpDaeModelImporter : ModelImporter
{
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.None;
    //PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices
    //| PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals; // TODO: what is needed?

    public AssimpDaeModelImporter(TextureFactory textureFactory)
        : base(textureFactory) { }

    public override Task<DefinedMeshData<VertexPositionNormalTextureColor, Index32>[]> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath, DeviceBufferPool? deviceBufferPool = null)
    {
        var directory = Path.GetDirectoryName(filePath);
        var importCollection = new List<DefinedMeshData<VertexPositionNormalTextureColor, Index32>>();
        var assimpContext = new AssimpContext();
        
        using var stream = File.OpenRead(filePath);
        var scene = assimpContext.ImportFileFromStream(stream, DefaultPostProcessSteps, Path.GetExtension(filePath));
        
        var instances = GetAllMeshInstances(scene, scene.RootNode, AssimpMatrix4x4.Identity).ToArray();
        var meshes = new List<DefinedMeshData<VertexPositionNormalTextureColor, Index32>>();
        for (var meshIndex = 0; meshIndex < scene.Meshes.Count; meshIndex++)
        {
            var mesh = scene.Meshes[meshIndex];
            var type = mesh.PrimitiveType == PrimitiveType.Point ? PrimitiveTopology.PointList :
                    mesh.PrimitiveType == PrimitiveType.Line ? PrimitiveTopology.LineList :
                    mesh.PrimitiveType == PrimitiveType.Triangle ? PrimitiveTopology.TriangleList :
                    throw new ArgumentException($"The mesh primitive type '{mesh.PrimitiveType}' is not supported");

            var specializations = new List<MeshDataSpecialization>();
            var shaderReadyVertices = new VertexPositionNormalTextureColor[mesh.VertexCount];
            var boneInfos = new BoneInfoVertex[mesh.VertexCount];
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

                boneInfos[i] = new BoneInfoVertex();
                shaderReadyVertices[i] = new VertexPositionNormalTextureColor(position, color, textureCordinate, normal);
            }

            if (mesh.HasBones)
            {
                var boneIdNames = new Dictionary<string, uint>();
                var transforms = Enumerable.Repeat(Matrix4x4.Identity, mesh.BoneCount).ToArray();
                for (uint boneId = 0; boneId < mesh.BoneCount; boneId++)
                {
                    var bone = mesh.Bones[(int)boneId];
                    boneIdNames.Add(bone.Name, boneId);
                    foreach (VertexWeight weight in bone.VertexWeights)
                    {
                        boneInfos[weight.VertexID].AddBone(boneId, weight.Weight);
                    }
                    transforms[boneId] = bone.OffsetMatrix.ToSystemMatrix();
                }

                specializations.Add(new BonesMeshDataSpecialization(boneInfos, transforms, LoadBoneAnimationProviders(transforms, scene, boneIdNames), deviceBufferPool));
            }

            var meshInstances = instances.Where(instance => instance.MeshIndex == meshIndex).ToArray();
            if (meshInstances.Any())
            {
                var instanceInfos = meshInstances.Select(instance =>
                {
                    Matrix4x4.Decompose(instance.Transform.Rotation, out _, out var rotation, out _);
                    return new InstanceInfo { Position = instance.Transform.Position, Rotation = rotation.ToEulerAngles(), Scale = instance.Transform.Scale, TexArrayIndex = 0 };
                }).ToArray();

                specializations.Add(new InstancedMeshDataSpecialization(instanceInfos, deviceBufferPool));
            }

            if (mesh.MaterialIndex > 0)
            {
                //TODO: load all textures            
                //TODO: load material file?
                var meshMaterial = scene.Materials[mesh.MaterialIndex];
                if (meshMaterial.HasTextureDiffuse)
                {
                    var path = Path.IsPathRooted(meshMaterial.TextureDiffuse.FilePath) || string.IsNullOrEmpty(directory) ? meshMaterial.TextureDiffuse.FilePath : Path.Combine(directory, meshMaterial.TextureDiffuse.FilePath);
                    specializations.Add(new SurfaceTextureMeshDataSpecialization(new DirectoryTextureProvider(TextureFactory, path)));
                }

                var material = new PhongMaterialInfo(
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
                specializations.Add(new PhongMaterialMeshDataSpecialization(material, meshMaterial.Name, deviceBufferPool));
            }

            var indices = mesh.GetUnsignedIndices().Select(x => (Index32)x).ToArray();
            meshes.Add(new DefinedMeshData<VertexPositionNormalTextureColor, Index32>(shaderReadyVertices, indices, type));
        }

        return Task.FromResult(meshes.ToArray());
    }

    private List<(int MeshIndex, Transform Transform)> GetAllMeshInstances(AssimpScene scene, Node node, AssimpMatrix4x4 baseTransform)
    {
        var meshTransforms = new List<(int MeshIndex, Transform Transform)>();
        var nodeTransform = node.Transform * baseTransform;

        foreach (var meshIndex in node.MeshIndices)
        {
            meshTransforms.Add((meshIndex, new Transform(nodeTransform.ToSystemMatrix())));
        }

        foreach (var child in node.Children)
        {
            meshTransforms.AddRange(GetAllMeshInstances(scene, child, nodeTransform));
        }
        return meshTransforms;
    }

    private IBoneAnimationProvider[] LoadBoneAnimationProviders(Matrix4x4[] boneTransforms, AssimpScene scene, Dictionary<string, uint> boneNames)
    {
        var rootInverseTransform = scene.RootNode.Transform;
        rootInverseTransform.Inverse();

        var boneAnimations = new List<IBoneAnimationProvider>();
        var assimpBoneTransforms = boneTransforms.Select(x => x.ToAssimpMatrix()).ToArray();
        foreach (var animation in scene.Animations)
        {
            // TODO: match animation to mesh!!
            boneAnimations.Add(new AssimpBoneAnimationProvider(animation, animation.NodeAnimationChannels, boneNames, assimpBoneTransforms, scene.RootNode, rootInverseTransform));
        }
        return boneAnimations.ToArray();
    }
}
using Assimp;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Numerics;
using Veldrid;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;

using Matrix4x4 = System.Numerics.Matrix4x4;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using AssimpScene = Assimp.Scene;
using NtFreX.BuildingBlocks.Standard;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public class AssimpDaeModelImporter : ModelImporter
{
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.None;
    //PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices
    //| PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals; // TODO: what is needed?

    public AssimpDaeModelImporter(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureFactory textureFactory, GraphicsSystem graphicsSystem)
        : base(graphicsDevice, resourceFactory, textureFactory, graphicsSystem) { }

    public override Task<ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>> PositionColorNormalTexture32BitMeshFromFileAsync(string filePath)
    {
        var importCollection = new ImportedMeshCollection<MeshDataProvider<VertexPositionNormalTextureColor, Index32>>();
        var assimpContext = new AssimpContext();
        using (var stream = File.OpenRead(filePath))
        {
            var scene = assimpContext.ImportFileFromStream(stream, DefaultPostProcessSteps, Path.GetExtension(filePath));
            var meshes = new List<(MeshDataProvider<VertexPositionNormalTextureColor, Index32> Mesh, Dictionary<string, uint> BoneNames)>();
            for (var meshIndex = 0; meshIndex < scene.Meshes.Count; meshIndex++)
            {
                var mesh = scene.Meshes[meshIndex];
                var type = mesh.PrimitiveType == PrimitiveType.Point ? PrimitiveTopology.PointList :
                        mesh.PrimitiveType == PrimitiveType.Line ? PrimitiveTopology.LineList :
                        mesh.PrimitiveType == PrimitiveType.Triangle ? PrimitiveTopology.TriangleList :
                        throw new ArgumentException();

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

                var boneIdNames = new Dictionary<string, uint>();
                var transforms = Enumerable.Repeat(Matrix4x4.Identity, mesh.BoneCount).ToArray(); //new Matrix4x4[DefaultMeshRenderPass.MaxBoneTransforms];
                if (mesh.HasBones)
                {
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

                var indices = mesh.GetUnsignedIndices().Select(x => (Index32)x).ToArray();
                var meshProvider = new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(shaderReadyVertices, indices, type, material: material, texturePath: texture)
                {
                    Bones = mesh.HasBones ? boneInfos : null,
                    BoneTransforms = mesh.HasBones ? transforms : null
                };
                meshes.Add((Mesh: meshProvider, BoneNames: boneIdNames));
            }

            SetBoneAnimationProviders(meshes, scene);
            importCollection.Instaces = ToMeshTransforms(scene, scene.RootNode, AssimpMatrix4x4.Identity).ToArray();
            importCollection.Meshes = meshes.Select(x => x.Mesh).ToArray();

            return Task.FromResult(importCollection);
        }
    }

    private List<MeshTransform> ToMeshTransforms(AssimpScene scene, Node node, AssimpMatrix4x4 baseTransform)
    {
        var meshTransforms = new List<MeshTransform>();
        var nodeTransform = node.Transform * baseTransform;

        foreach (var meshIndex in node.MeshIndices)
        {
            meshTransforms.Add(new MeshTransform { MeshIndex = (uint) meshIndex, Transform = new Transform(nodeTransform.ToSystemMatrix()) });
        }

        foreach (var child in node.Children)
        {
            meshTransforms.AddRange(ToMeshTransforms(scene, child, nodeTransform));
        }
        return meshTransforms;
    }

    private void SetBoneAnimationProviders(List<(MeshDataProvider<VertexPositionNormalTextureColor, Index32> Mesh, Dictionary<string, uint> BoneNames)> meshes, AssimpScene scene)
    {
        var rootInverseTransform = scene.RootNode.Transform;
        rootInverseTransform.Inverse();

        for(int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
        {
            var boneAnimations = new List<IBoneAnimationProvider>();
            foreach (var animation in scene.Animations)
            {
                var boneTransForms = meshes[meshIndex].Mesh.BoneTransforms;
                if (boneTransForms != null)
                {
                    var transformBuffer = new Matrix4x4[DefaultMeshRenderPass.MaxBoneTransforms];
                    boneTransForms.CopyTo(transformBuffer, 0);

                    boneAnimations.Add(new AssimpBoneAnimationProvider(transformBuffer, animation, animation.NodeAnimationChannels, meshes[meshIndex].BoneNames, boneTransForms.Select(x => x.ToAssimpMatrix()).ToArray(), scene.RootNode, rootInverseTransform));
                }
            }

            meshes[meshIndex].Mesh.BoneAnimationProviders = boneAnimations.ToArray();
        }
    }
}
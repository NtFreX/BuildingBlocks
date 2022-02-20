using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public static class DaeFileReader
{
    public class DaeFile
    {
        public readonly Mesh[] Meshes;
        public readonly Scene[] Scenes;
        public readonly Material[] Materials;

        public DaeFile(Mesh[] meshes, Scene[] scenes, Material[] materials)
        {
            Meshes = meshes;
            Scenes = scenes;
            Materials = materials;
        }
    }
    public class Scene
    {
        public readonly Node[] Nodes;

        public Scene(Node[] nodes)
        {
            Nodes = nodes;
        }
    }
    public class Node
    {
        public readonly Matrix4x4 Transform;
        public readonly Node[] Children;
        public readonly NodeGeometry[] InstanceMeshes;

        public Node(Matrix4x4 transform, Node[] children, NodeGeometry[] instanceMeshes)
        {
            Transform = transform;
            Children = children;
            InstanceMeshes = instanceMeshes;
        }
    }
    public class NodeGeometry
    {
        public readonly string Name;
        public readonly string? MaterialName;

        public NodeGeometry(string name, string? materialName)
        {
            Name = name;
            MaterialName = materialName;
        }
    }
    public class Mesh
    {
        public readonly string Id;
        public readonly string? Material;
        public readonly float[] Positions;
        public readonly float[] Normals;
        public readonly float[] TexCoords;
        public readonly float[] Colors;
        public readonly uint[] Indices;
        public readonly VertexLayoutDescription Layout;

        public Mesh(string id, string? material, float[] positions, float[] normals, float[] texCoords, float[] colors, uint[] indices, VertexLayoutDescription layout)
        {
            Id = id;
            Material = material;
            Positions = positions;
            Normals = normals;
            TexCoords = texCoords;
            Colors = colors;
            Indices = indices;
            Layout = layout;
        }
    }
    public class Material
    {
        public readonly string Name;
        public readonly Vector3? DiffuseColor;
        public readonly string? DiffuseTexture;

        public Material(string name, Vector3? diffuseColor, string? diffuseTexture)
        {
            Name = name;
            DiffuseColor = diffuseColor;
            DiffuseTexture = diffuseTexture;
        }
    }

    enum VertexSemantic
    {
        VERTEX,
        NORMAL,
        TEXCOORD,
        COLOR
    }

    private static readonly XNamespace Name = "http://www.collada.org/2005/11/COLLADASchema";

    public static DaeFile LoadFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadStream(stream);
    }
    public static DaeFile LoadStream(Stream stream)
    {
        // TODO: make async work
        var document = XDocument.Load(stream);
        return LoadDocument(document);
    }
    public static DaeFile LoadDocument(XDocument document)
    {
        //TODO: why the f does xpath not work
        XNamespace name = "http://www.collada.org/2005/11/COLLADASchema";
        var root = document.Element(name + "COLLADA");
        var meshes = LoadMeshes(root?.Element(name + "library_geometries"));
        var scenes = LoadScenes(root?.Element(name + "library_visual_scenes"));
        var materials = LoadMaterials(root);
        return new DaeFile(meshes, scenes, materials);
    }

    private static Material[] LoadMaterials(XElement? root)
    {
        var materials = root?.Element(Name + "library_materials")?.Elements(Name + "material") ?? Array.Empty<XElement>();
        var effects = root?.Element(Name + "library_effects")?.Elements(Name + "effect") ?? Array.Empty<XElement>();
        var images = root?.Element(Name + "library_images")?.Elements(Name + "image")?.Select(image => (Id: image.Attribute("id")?.Value ?? throw new Exception(), Path: image.Element(Name + "init_from")?.Value ?? throw new Exception())).ToArray();

        return materials.Select(material =>
        {
            var effectUrl = material.Element(Name + "instance_effect")?.Attribute("url")?.Value ?? string.Empty;
            var effect = effects.First(effect => effect.Attribute("id")?.Value == effectUrl.Replace("#", ""));
            var commonProfile = effect.Element(Name + "profile_COMMON");
            var commonTechinque = commonProfile?.Elements(Name + "technique").First(x => x.Attribute("sid")?.Value == "common") ?? throw new Exception();
            var lambertElement = commonTechinque.Element(Name + "lambert");
            var diffuseTexture = lambertElement?.Element(Name + "diffuse")?.Element(Name + "texture")?.Attribute("texture")?.Value ?? null;
            var diffuseSampler = diffuseTexture == null ? null : ResolveSampler(commonProfile, diffuseTexture);
            var diffuseSurface = diffuseSampler == null ? null : ResolveSurface2D(commonProfile, diffuseSampler);
            var diffuseColor = ParseColor(lambertElement?.Element(Name + "diffuse")?.Element(Name + "color")?.Value ?? string.Empty);
            var diffiuseTexture = images?.FirstOrDefault(img => img.Id == diffuseSurface).Path;

            return new Material(material.Attribute("id")?.Value ?? throw  new Exception(), diffuseColor, diffiuseTexture);
        }).ToArray();
    }

    private static Vector3? ParseColor(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var parts = value.Split(' ');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }
    private static string ResolveSurface2D(XElement root, string name)
        => root.Elements(Name + "newparam").First(x => x.Attribute("sid")?.Value == name)?.Element(Name + "surface")?.Element(Name + "init_from")?.Value ?? throw new Exception();
    private static string ResolveSampler(XElement root, string name)
        => root.Elements(Name + "newparam").First(x => x.Attribute("sid")?.Value == name)?.Element(Name + "sampler2D")?.Element(Name + "source")?.Value ?? throw new Exception();

    private static Scene[] LoadScenes(XElement? sceneElement)
    {
        var scenes = sceneElement?.Elements(Name + "visual_scene") ?? Array.Empty<XElement>();
        return scenes.Select(x =>
        {
            var nodes = x.Elements(Name + "node")?.Select(node => LoadNode(node)).ToArray() ?? Array.Empty<Node>();
            return new Scene(nodes);
        }).ToArray();
    }

    private static Node LoadNode(XElement rootNode)
    {
        var transformText = rootNode.Element(Name + "matrix")?.Value;
        var transform = string.IsNullOrEmpty(transformText) ? Matrix4x4.Identity : ToMatrix(transformText.Split(' ').Select(x => float.Parse(x)).ToArray());
        var instanceGeometries = rootNode.Elements(Name + "instance_geometry").Select(x => {
            var name = x.Attribute("url")?.Value ?? throw new Exception("The given instance geometry has no url attribute");
            var materialName = x.Element(Name + "bind_material")?.Element(Name + "technique_common")?.Element(Name + "instance_material")?.Attribute("symbol")?.Value ?? null;
            return new NodeGeometry(name, materialName);
        }).ToArray();
        var children = new List<Node>();
        foreach(var element in rootNode.Elements(Name + "node"))
        {
            children.Add(LoadNode(element));
        }
        return new Node(transform, children.ToArray(), instanceGeometries);
    }

    private static Mesh[] LoadMeshes(XElement? geometriesElement)
    {
        var geometries = geometriesElement?.Elements(Name + "geometry") ?? Array.Empty<XElement>();
        return geometries.SelectMany(x =>
        {
            var id = x.Attribute("id")?.Value;
            var triangles = x.Element(Name + "mesh")?.Elements(Name + "triangles") ?? Array.Empty<XElement>();
            return triangles.Select(triangle =>
            {
                var material = triangle?.Attribute("material")?.Value;
                var indices = triangle?.Element(Name + "p")?.Value.Split(' ').Select(x => uint.Parse(x)).ToArray() ?? Array.Empty<uint>();
                var inputs = x.Element(Name + "mesh")?.Elements(Name + "source").ToArray() ?? Array.Empty<XElement>();
                var positions = Array.Empty<float>();
                var normals = Array.Empty<float>();
                var texCoords = Array.Empty<float>();
                var colors = Array.Empty<float>();
                var inputDefinitions = triangle?.Elements(Name + "input") ?? Array.Empty<XElement>();

                var vertexIndex = 0;
                var layoutIndex = 0;
                var layoutElements = new List<VertexElementDescription>();
                foreach (var inputDefinition in inputDefinitions)
                {
                    var attribute = inputDefinition.Attributes().FirstOrDefault(x => x.Name == "semantic");
                    var offset = uint.Parse(inputDefinition.Attribute("offset")?.Value ?? "0");
                    if (attribute?.Value == VertexSemantic.VERTEX.ToString())
                    {
                        positions = GetValues(Name, inputs, inputDefinition);
                        var stride = GetStride(Name, inputs, inputDefinition);
                        layoutElements.Add(new VertexElementDescription(VertexElementSemantic.Position.ToString(), VertexElementSemantic.Position, GetElementFormat(stride), offset));
                        vertexIndex = layoutIndex;
                    }
                    if (attribute?.Value == VertexSemantic.NORMAL.ToString())
                    {
                        normals = GetValues(Name, inputs, inputDefinition);
                        var stride = GetStride(Name, inputs, inputDefinition);
                        layoutElements.Add(new VertexElementDescription(VertexElementSemantic.Normal.ToString(), VertexElementSemantic.Normal, GetElementFormat(stride), offset));
                    }
                    if (attribute?.Value == VertexSemantic.TEXCOORD.ToString())
                    {
                        texCoords = GetValues(Name, inputs, inputDefinition);
                        var stride = GetStride(Name, inputs, inputDefinition);
                        layoutElements.Add(new VertexElementDescription(VertexElementSemantic.TextureCoordinate.ToString(), VertexElementSemantic.TextureCoordinate, GetElementFormat(stride), offset));
                    }
                    if (attribute?.Value == VertexSemantic.COLOR.ToString())
                    {
                        colors = GetValues(Name, inputs, inputDefinition);
                        var stride = GetStride(Name, inputs, inputDefinition);
                        layoutElements.Add(new VertexElementDescription(VertexElementSemantic.Color.ToString(), VertexElementSemantic.Color, GetElementFormat(stride), offset));
                    }
                    layoutIndex++;
                }

                var layoutCount = layoutElements.ToArray().Length;
                return new Mesh(id, material, positions, normals, texCoords, colors, indices, new VertexLayoutDescription(layoutElements.ToArray()));
            });
        }).ToArray();
    }

    private static Matrix4x4 ToMatrix(float[] values)
    {
        return new Matrix4x4(
            values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]);
    }

    private static VertexElementFormat GetElementFormat(int stride)
        => stride == 2 ? VertexElementFormat.Float2 :
            stride == 3 ? VertexElementFormat.Float3 :
            stride == 4 ? VertexElementFormat.Float4 : throw new NotSupportedException();

    private static int GetStride(XNamespace name, XElement[] inputs, XElement channel)
        => int.Parse(inputs[GetOffset(channel)].Element(name + "technique_common")?.Element(name + "accessor")?.Attribute("stride")?.Value ?? "0");
    private static float[] GetValues(XNamespace name, XElement[] inputs, XElement channel)
        => GetValues(name, inputs, GetOffset(channel)).Select(x => float.Parse(x)).ToArray();
    private static string[] GetValues(XNamespace name, XElement[] inputs, int offset)
        => inputs[offset].Element(name + "float_array")?.Value.Split(' ') ?? throw new Exception("The given offset for the channel seems to be wrong");
    private static int GetOffset(XElement element) 
        => int.Parse(element.Attribute("offset")?.Value ?? throw new Exception("The element geometry/mesh/triangles/input must have an offset attribute"));

    private static List<Node> AggregateNodes(Node[] nodes, Matrix4x4 baseTransform)
    {
        var allNodes = new List<Node>();
        foreach (var node in nodes)
        {
            var nodeTransform = baseTransform * node.Transform;
            allNodes.Add(new Node(nodeTransform, Array.Empty<Node>(), node.InstanceMeshes));
            foreach (var child in AggregateNodes(node.Children, nodeTransform))
            {
                allNodes.Add(new Node(child.Transform * nodeTransform, Array.Empty<Node>(), child.InstanceMeshes));
            }
        }
        return allNodes;
    }

    private static uint GetMeshIndex(DaeFile daeFile, string meshName)
    {
        var meshID = meshName.Replace("#", "");
        for (uint i = 0; i < daeFile.Meshes.Length; i++)
        {
            if (daeFile.Meshes[i].Id == meshID)
                return i;
        }
        throw new Exception($"A mesh with the name {meshName} was not found");
    }
    private static string? GetSurfaceTexture(DaeFile daeFile, string? name)
    {
        if (name == null)
            return null;

        return daeFile.Materials.First(x => x.Name == name).DiffuseTexture;
    }
    private static MaterialInfo? GetMaterial(DaeFile daeFile, string? name)
    {
        if (name == null)
            return null;

        var material = new MaterialInfo();
        var color = daeFile.Materials.First(x => x.Name == name).DiffuseColor;
        if (color != null)
            material = material with { DiffuseColor = new Vector4(color.Value, 1) };

        return material;
    }

    public static Task<ImportedMeshCollection<BinaryMeshDataProvider>> BinaryMeshFromFileAsync(string filePath)
    {
        var daeFile = LoadFile(filePath);
        var collection = new ImportedMeshCollection<BinaryMeshDataProvider>();
        var nodesAggregated = daeFile.Scenes.SelectMany(scene => AggregateNodes(scene.Nodes, Matrix4x4.Identity));
        collection.Meshes = daeFile.Meshes.Select(mesh => BinaryMeshDataProvider.Create(mesh.Positions, mesh.Normals, mesh.TexCoords, mesh.Colors, mesh.Indices, mesh.Layout)).ToArray();
        collection.Instaces = nodesAggregated.SelectMany(node => node.InstanceMeshes.Select(nodeGeometry => new MeshTransform { 
            MeshIndex = GetMeshIndex(daeFile, nodeGeometry.Name), 
            Transform = new Transform(node.Transform), 
            SurfaceTexture = GetSurfaceTexture(daeFile, nodeGeometry.MaterialName),
            Material = GetMaterial(daeFile, nodeGeometry.MaterialName) })).ToArray();
        return Task.FromResult(collection);
    }
}
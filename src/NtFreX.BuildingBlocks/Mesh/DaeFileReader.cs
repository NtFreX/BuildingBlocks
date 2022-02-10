using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public static class DaeFileReader
{
    public class DaeFile
    {

        public readonly Mesh[] Meshes;
        public readonly Scene[] Scenes;

        public DaeFile(Mesh[] meshes, Scene[] scenes)
        {
            Meshes = meshes;
            Scenes = scenes;
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
        public readonly string[] InstanceMeshes;

        public Node(Matrix4x4 transform, Node[] children, string[] instanceMeshes)
        {
            Transform = transform;
            Children = children;
            InstanceMeshes = instanceMeshes;
        }
    }
    public class Mesh
    {
        public readonly string Id;
        public readonly float[] Positions;
        public readonly float[] Normals;
        public readonly float[] TexCoords;
        public readonly float[] Colors;
        public readonly uint[] Indices;
        public readonly VertexLayoutDescription Layout;

        public Mesh(string id, float[] positions, float[] normals, float[] texCoords, float[] colors, uint[] indices, VertexLayoutDescription layout)
        {
            Id = id;
            Positions = positions;
            Normals = normals;
            TexCoords = texCoords;
            Colors = colors;
            Indices = indices;
            Layout = layout;
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
        return new DaeFile(meshes, scenes);
    }

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
        var instanceGeometries = rootNode.Elements(Name + "instance_geometry").Select(x => x.Attribute("url").Value).ToArray();
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
        return geometries.Select(x =>
        {
            var id = x.Attribute("id")?.Value;
            var triangles = x.Element(Name + "mesh")?.Element(Name + "triangles");
            var indices = triangles?.Element(Name + "p")?.Value.Split(' ').Select(x => uint.Parse(x)).ToArray() ?? Array.Empty<uint>();
            var inputs = x.Element(Name + "mesh")?.Elements(Name + "source").ToArray() ?? Array.Empty<XElement>();
            var positions = Array.Empty<float>();
            var normals = Array.Empty<float>();
            var texCoords = Array.Empty<float>();
            var colors = Array.Empty<float>();
            var inputDefinitions = triangles?.Elements(Name + "input") ?? Array.Empty<XElement>();

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
            return new Mesh(id, positions, normals, texCoords, colors, indices, new VertexLayoutDescription(layoutElements.ToArray()));
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

    public static Task<BinaryMeshDataProvider[]> BinaryMeshFromFileAsync(string filePath)
    {
        var daeFile = LoadFile(filePath);
        var meshes = new List<BinaryMeshDataProvider>();
        var importedMeshes = daeFile.Scenes.SelectMany(scene => AggregateNodes(scene.Nodes)).SelectMany(node => node.InstanceMeshes.Select(meshName => (Mesh: daeFile.Meshes.First(mesh => mesh.Id == meshName.Replace("#", "")), Transform: node.Transform))).ToArray();
        for (var meshIndex = 0; meshIndex < importedMeshes.Length; meshIndex++)
        {
            // TODO: index16 support?
            // TODO: read material
            var binaryMesh = BinaryMeshDataProvider.Create(importedMeshes[meshIndex].Mesh.Positions, importedMeshes[meshIndex].Mesh.Normals, importedMeshes[meshIndex].Mesh.TexCoords, importedMeshes[meshIndex].Mesh.Colors, importedMeshes[meshIndex].Mesh.Indices, importedMeshes[meshIndex].Mesh.Layout);
            binaryMesh.Transform = importedMeshes[meshIndex].Transform;
            meshes.Add(binaryMesh);
        }
        return Task.FromResult(meshes.ToArray());
    }

    private static List<Node> AggregateNodes(Node[] nodes)
    {
        var allNodes = new List<Node>(nodes);
        foreach (var node in nodes)
        {
            var children = AggregateNodes(node.Children);
            var transformedChildren = new List<Node>();
            foreach(var child in children)
            {
                transformedChildren.Add(new Node(child.Transform * node.Transform, Array.Empty<Node>(), child.InstanceMeshes));
            }
            allNodes.AddRange(transformedChildren);
        }
        return allNodes;
    }
}
using NtFreX.BuildingBlocks.Mesh.Primitives;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

using static Veldrid.Utilities.ObjFile;

namespace NtFreX.BuildingBlocks.Mesh.Import;

public static class ObjFileExtensions
{
    public static (VertexPositionNormalTextureColor[] Vertices, Index32[] Indices) GetData(this ObjFile objFile, MeshGroup group, RgbaFloat color)
    {
        var vertexMap = new Dictionary<FaceVertex, uint>();
        var indices = new Index32[group.Faces.Length * 3];
        var vertices = new List<VertexPositionNormalTextureColor>();

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
        List<VertexPositionNormalTextureColor> vertices,
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

    private static VertexPositionNormalTextureColor ConstructVertex(ObjFile objFile, FaceVertex key, FaceVertex adjacent1, FaceVertex adjacent2, RgbaFloat color)
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

        return new VertexPositionNormalTextureColor(position, color, texCoord, normal);
    }

    private static Vector3 ComputeNormal(ObjFile objFile, FaceVertex v1, FaceVertex v2, FaceVertex v3)
    {
        Vector3 pos1 = objFile.Positions[v1.PositionIndex - 1];
        Vector3 pos2 = objFile.Positions[v2.PositionIndex - 1];
        Vector3 pos3 = objFile.Positions[v3.PositionIndex - 1];

        return Vector3.Normalize(Vector3.Cross(pos1 - pos2, pos1 - pos3));
    }
}

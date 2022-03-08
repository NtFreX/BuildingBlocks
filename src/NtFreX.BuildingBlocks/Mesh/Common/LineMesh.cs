using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class LineMesh
{
    public static DefinedMeshData<VertexPositionNormalTextureColor, Index16> CreateMesh(Vector3 start, Vector3 end, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f)
    {
        //TODO: normals for lines?
        var color = new RgbaFloat(red, green, blue, alpha);
        var vertices = new[] { new VertexPositionNormalTextureColor(start, color), new VertexPositionNormalTextureColor(end, color) };
        var indices = new Index16[] { 0, 1 };
        return new DefinedMeshData<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.LineList);
    }

    public static Task<MeshRenderer> CreateAsync(
        Vector3 start, Vector3 end, Transform? transform = null, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, string? name = null, 
        DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    {
        var mesh = CreateMesh(start, end, red, green, blue, alpha);
        return MeshRenderer.CreateAsync(new StaticMeshDataProvider(mesh), transform: transform, name: name, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
    }
}


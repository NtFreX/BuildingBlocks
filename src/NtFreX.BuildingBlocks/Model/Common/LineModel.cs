using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model.Common;

public static class LineModel
{
    public static MeshDataProvider<VertexPositionNormalTextureColor, Index16> CreateMesh(Vector3 start, Vector3 end, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, MaterialInfo? material = null)
    {
        var color = new RgbaFloat(red, green, blue, alpha);
        var vertices = new[] { new VertexPositionNormalTextureColor(start, color), new VertexPositionNormalTextureColor(end, color) };
        var indices = new Index16[] { 0, 1 };
        return new MeshDataProvider<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.LineList, material: material);
    }

    public static MeshRenderer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Vector3 start, Vector3 end, Transform? transform = null,
        float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, string? name = null, MaterialInfo? material = null, DeviceBufferPool? deviceBufferPool = null)
    {
        var mesh = CreateMesh(start, end, red, green, blue, alpha, material);
        return MeshRenderer.Create(graphicsDevice, resourceFactory, graphicsSystem, mesh, transform: transform, name: name, deviceBufferPool: deviceBufferPool);
    }
}


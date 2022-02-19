using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class QubeModel
{
    public static MeshDataProvider<VertexPositionNormalTextureColor, Index16> CreateMesh(float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float sideLength = 1f, MaterialInfo? material = null)
    {
        var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), sideLength / 2f);
        var indices = GetIndices();
        return new MeshDataProvider<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.TriangleList, material: material);
    }

    public static MeshRenderer Create(
        GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Transform? transform = null,
        float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float sideLength = 1f, TextureView? texture = null, MaterialInfo? material = null,
        string? name = null, DeviceBufferPool? deviceBufferPool = null)
    {
        var realTransform = transform ?? new Transform();
        var mesh = CreateMesh(red, green, blue, alpha, sideLength, material);
        var shape = new Box(sideLength * realTransform.Scale.X, sideLength * realTransform.Scale.Y, sideLength * realTransform.Scale.Z);
        return MeshRenderer.Create(graphicsDevice, resourceFactory, graphicsSystem, mesh, shape, transform: realTransform, textureView: texture, name: name, deviceBufferPool: deviceBufferPool);
    }

    private static VertexPositionNormalTextureColor[] GetVertices(RgbaFloat color, float halfSideLength)
    {
        var vertexOne = new Vector3(-halfSideLength, +halfSideLength, -halfSideLength);
        var vertexTwo = new Vector3(+halfSideLength, +halfSideLength, -halfSideLength);
        var vertexThree = new Vector3(+halfSideLength, +halfSideLength, +halfSideLength);
        var vertexFour = new Vector3(-halfSideLength, +halfSideLength, +halfSideLength);
        var vertexFive = new Vector3(-halfSideLength, -halfSideLength, +halfSideLength);
        var vertexSix = new Vector3(-halfSideLength, -halfSideLength, -halfSideLength);
        var vertexSeven = new Vector3(+halfSideLength, -halfSideLength, -halfSideLength);
        var vertexEight = new Vector3(+halfSideLength, -halfSideLength, +halfSideLength);

        return new [] {
            // Top
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(0, 1)),

            // Bottom 
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(0, 1)),

            // Left  
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(0, 1)),

            // Right
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(0, 1)),

            // Back
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(0, 1)),

            // Front
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(0, 0)),
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(1, 0)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(1, 1)),
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(0, 1)),
        };
    }

    private static Index16[] GetIndices() => new Index16[] {                 
        0,1,2, 0,2,3,
        4,5,6, 4,6,7,
        8,9,10, 8,10,11,
        12,13,14, 12,14,15,
        16,17,18, 16,18,19,
        20,21,22, 20,22,23, 
    };
}

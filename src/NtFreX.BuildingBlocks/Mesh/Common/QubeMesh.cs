using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class QubeMesh
{
    public static DefinedMeshData<VertexPositionNormalTextureColor, Index16> CreateMesh(float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float sideLength = 1f)
    {
        var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), sideLength / 2f);
        var indices = GetIndices();
        return new DefinedMeshData<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.TriangleList, faceCullMode: FaceCullMode.Front);
    }

    public static Task<MeshRenderer> CreateAsync(
        Transform? transform = null, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float sideLength = 1f, 
        string? name = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, MeshDataSpecialization[]? specializations = null)
    {
        var mesh = CreateMesh(red, green, blue, alpha, sideLength);

        var scale = transform?.Scale ?? Vector3.One;
        var shape = new Box(sideLength * scale.X, sideLength * scale.Y, sideLength * scale.Z);
        mesh.Specializations.AddOrUpdate(new BepuPhysicsShapeMeshDataSpecialization<Box>(shape));
        
        if(specializations != null)
            mesh.Specializations.AddOrUpdate(specializations);
        
        return MeshRenderer.CreateAsync(new StaticMeshDataProvider(mesh), transform: transform, name: name, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
    }

    private static VertexPositionNormalTextureColor[] GetVertices(RgbaFloat color, float halfSideLength)
    {
        var vertexOne = new Vector3(-halfSideLength, +halfSideLength, -halfSideLength);
        var vertexTwo = new Vector3(+halfSideLength, +halfSideLength, -halfSideLength);
        var vertexThree = new Vector3(+halfSideLength, +halfSideLength, +halfSideLength);
        var vertexFour = new Vector3(-halfSideLength, +halfSideLength, +halfSideLength);
        var vertexFive = new Vector3(-halfSideLength, -halfSideLength, +halfSideLength);
        var vertexEight = new Vector3(-halfSideLength, -halfSideLength, -halfSideLength);
        var vertexSeven = new Vector3(+halfSideLength, -halfSideLength, -halfSideLength);
        var vertexSix = new Vector3(+halfSideLength, -halfSideLength, +halfSideLength);

        return new [] {
            // Top
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(0, 0), new Vector3(0, 1, 0)),
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(1, 0), new Vector3(0, 1, 0)),
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(1, 1), new Vector3(0, 1, 0)),
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(0, 1), new Vector3(0, 1, 0)),

            // Bottom 
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(0, 0), new Vector3(0, -1, 0)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(1, 0), new Vector3(0, -1, 0)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(1, 1), new Vector3(0, -1, 0)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(0, 1), new Vector3(0, -1, 0)),

            // Left  
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(0, 0), new Vector3(1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(1, 0), new Vector3(1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(1, 1), new Vector3(1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(0, 1), new Vector3(1, 0, 0)),

            // Right
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(0, 0), new Vector3(-1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(1, 0), new Vector3(-1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(1, 1), new Vector3(-1, 0, 0)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(0, 1), new Vector3(-1, 0, 0)),

            // Back
            new VertexPositionNormalTextureColor(vertexTwo, color, new Vector2(0, 0), new Vector3(0, 0, 1)),
            new VertexPositionNormalTextureColor(vertexOne, color, new Vector2(1, 0), new Vector3(0, 0, 1)),
            new VertexPositionNormalTextureColor(vertexEight, color, new Vector2(1, 1), new Vector3(0, 0, 1)),
            new VertexPositionNormalTextureColor(vertexSeven, color, new Vector2(0, 1), new Vector3(0, 0, 1)),

            // Front
            new VertexPositionNormalTextureColor(vertexFour, color, new Vector2(0, 0), new Vector3(0, 0, -1)),
            new VertexPositionNormalTextureColor(vertexThree, color, new Vector2(1, 0), new Vector3(0, 0, -1)),
            new VertexPositionNormalTextureColor(vertexSix, color, new Vector2(1, 1), new Vector3(0, 0, -1)),
            new VertexPositionNormalTextureColor(vertexFive, color, new Vector2(0, 1), new Vector3(0, 0, -1)),
        };
    }

    private static Index16[] GetIndices() => new Index16[] {                 
        2,1,0, 3,2,0,
        6,5,4, 7,6,4,
        10,9,8, 11,10,8,
        14,13,12, 15,14,12,
        18,17,16, 19,18,16,
        22,21,20, 23,22,20,
    };
}

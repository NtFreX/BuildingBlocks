using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid;

using BepuBufferPool = BepuUtilities.Memory.BufferPool;
using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class PlaneMesh
{
    public static DefinedMeshData<VertexPositionNormalTextureColor, Index16> CreateMesh(float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, int rows = 2, int columns = 2)
    {
        if (rows < 2)
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows need to be bigger then 1");
        if (columns < 2)
            throw new ArgumentOutOfRangeException(nameof(rows), "Columns need to be bigger then 1");

        var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), rows, columns);
        var indices = GetIndices(rows, columns);
        return new DefinedMeshData<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.TriangleList, faceCullMode: FaceCullMode.Front);
    }

    public static Task<MeshRenderer> CreateAsync(
        Transform? transform = null, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, int rows = 2, int columns = 2,
        string? name = null, DeviceBufferPool? deviceBufferPool = null, BepuBufferPool? physicsBufferPool = null, CommandListPool? commandListPool = null, MeshDataSpecialization[]? specializations = null)
    {
        var mesh = CreateMesh(red, green, blue, alpha, rows, columns);
        
        if (physicsBufferPool != null)
        {
            var shape = mesh.GetPhysicsMesh(physicsBufferPool, transform != null ? transform.Value.Scale : Vector3.One);
            mesh.Specializations.AddOrUpdate(new BepuPhysicsShapeMeshDataSpecialization<BepuPhysicsMesh>(shape));
        }

        if (specializations != null)
            mesh.Specializations.AddOrUpdate(specializations);

        return MeshRenderer.CreateAsync(new StaticMeshDataProvider(mesh), transform: transform, name: name, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
    }

    private static VertexPositionNormalTextureColor[] GetVertices(RgbaFloat color, int rows, int columns)
    {
        var vertices = new List<VertexPositionNormalTextureColor>();
        var halfRows = -(rows / 2f);
        var halfColumns = -(columns / 2f);
        for (float i = 0; i < rows; i++)
        {
            for (float j = 0; j < columns; j++)
            {
                //TODO: fix normal
                vertices.Add(new VertexPositionNormalTextureColor(new Vector3(i + halfRows, 0, j + halfColumns), color, new Vector2(i + i % 1 - rows, j + j % 1 - columns), new Vector3(0, -1f, 0)));
            }
        }
        return vertices.ToArray();
    }

    private static Index16[] GetIndices(int rows, int columns)
    {
        var indices = new List<Index16>();
        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < columns - 1; j++)
            {
                var one = columns * i + j;
                indices.Add(one + 1);
                indices.Add(one + columns + 1);
                indices.Add(one);

                indices.Add(one);
                indices.Add(one + columns + 1);
                indices.Add(one + columns);
            }
        }
        return indices.ToArray();
    }
}

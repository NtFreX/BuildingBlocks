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

public static class SphereMesh
{
    public static DefinedMeshData<VertexPositionNormalTextureColor, Index16> CreateMesh(
        float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float radius = 1f, 
        int sectorCount = 5, int stackCount = 5)
    {
        var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), radius, sectorCount, stackCount);
        var indices = GetIndices(sectorCount, stackCount);
        return new DefinedMeshData<VertexPositionNormalTextureColor, Index16>(vertices, indices, PrimitiveTopology.TriangleList, faceCullMode: FaceCullMode.Front);
    }

    public static Task<MeshRenderer> CreateAsync(
        Transform? transform = null, float red = 0f, float green = 0f, float blue = 0f, float alpha = 1f, float radius = 1f, 
        int sectorCount = 5, int stackCount = 5, string? name = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, MeshDataSpecialization[]? specializations = null)
    {
        var mesh = CreateMesh(red, green, blue, alpha, radius, sectorCount, stackCount);
        mesh.Specializations.AddOrUpdate(new BepuPhysicsShapeMeshDataSpecialization<Sphere>(new Sphere(radius)));

        if (specializations != null)
            mesh.Specializations.AddOrUpdate(specializations);

        return MeshRenderer.CreateAsync(new StaticMeshDataProvider(mesh), transform: transform, name: name, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
    }

    private static VertexPositionNormalTextureColor[] GetVertices(RgbaFloat color, float radius, int sectorCount, int stackCount)
    {
        // http://www.songho.ca/opengl/gl_sphere.html
        var vertices = new List<VertexPositionNormalTextureColor>();

        float x, y, z, xy;                              // vertex position
        float nx, ny, nz, lengthInv = 1.0f / radius;    // vertex normal
        float s, t;                                     // vertex texCoord

        double sectorStep = 2f * Math.PI / sectorCount;
        double stackStep = Math.PI / stackCount;
        double sectorAngle, stackAngle;

        for (int i = 0; i <= stackCount; ++i)
        {
            stackAngle = Math.PI / 2f - i * stackStep;      // starting from pi/2 to -pi/2
            xy = (float)(radius * Math.Cos(stackAngle));             // r * cos(u)
            z = (float)(radius * Math.Sin(stackAngle));              // r * sin(u)

            // add (sectorCount+1) vertices per stack
            // the first and last vertices have same position and normal, but different tex coords
            for (int j = 0; j <= sectorCount; ++j)
            {
                sectorAngle = j * sectorStep;           // starting from 0 to 2pi

                // vertex position (x, y, z)
                x = (float)(xy * Math.Cos(sectorAngle));             // r * cos(u) * cos(v)
                y = (float)(xy * Math.Sin(sectorAngle));             // r * cos(u) * sin(v)


                // vertex tex coord (s, t) range between [0, 1]
                s = (float)j / sectorCount;
                t = (float)i / stackCount;

                var position = new Vector3(x, y, z);
                nx = position.X * lengthInv;
                ny = position.Y * lengthInv;
                nz = position.Z * lengthInv;

                vertices.Add(new VertexPositionNormalTextureColor(
                    position,
                    color,
                    new Vector2(s, t),
                    new Vector3(nx, ny, nz)));
            }
        }

        return vertices.ToArray();
    }

    private static Index16[] GetIndices(int sectorCount, int stackCount)
    {
        var indices = new List<Index16>();
        int k1, k2;
        for (int i = 0; i < stackCount; ++i)
        {
            k1 = i * (sectorCount + 1);     // beginning of current stack
            k2 = k1 + sectorCount + 1;      // beginning of next stack

            for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
            {
                // 2 triangles per sector excluding first and last stacks
                // k1 => k2 => k1+1
                if (i != 0)
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }

                // k1+1 => k2 => k2+1
                if (i != (stackCount - 1))
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }

        return indices.ToArray();
    }
}

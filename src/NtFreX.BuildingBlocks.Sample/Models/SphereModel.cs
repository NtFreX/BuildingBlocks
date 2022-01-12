using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    static class SphereModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, ushort> CreateMesh(
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float radius = 1f, 
            int sectorCount = 5, int stackCount = 5, MaterialInfo? material = null)
        {
            var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), radius, sectorCount, stackCount);
            var indices = GetIndices(sectorCount, stackCount);
            return new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(
                vertices, indices, IndexFormat.UInt16, PrimitiveTopology.TriangleList,
                VertexPositionColorNormalTexture.VertexLayout, material: material,
                bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition);
        }

        public static Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, ModelCreationInfo creationInfo, Shader[] shaders,
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float radius = 1f, int sectorCount = 5, int stackCount = 5, TextureView? texture = null, MaterialInfo? material = null,
            string? name = null)
        {
            var mesh = CreateMesh(red, green, blue, alpha, radius, sectorCount, stackCount, material);
            var shapeAllocator = (Simulation simulation) => new Sphere(radius);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders, mesh, shapeAllocator, textureView: texture, name: name);
        }

        private static VertexPositionColorNormalTexture[] GetVertices(RgbaFloat color, float radius, int sectorCount, int stackCount)
        {
            // http://www.songho.ca/opengl/gl_sphere.html
            var vertices = new List<VertexPositionColorNormalTexture>();

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

                    vertices.Add(new VertexPositionColorNormalTexture(
                        position,
                        color,
                        new Vector2(s, t),
                        new Vector3(nx, ny, nz)));
                }
            }

            return vertices.ToArray();
        }

        private static ushort[] GetIndices(int sectorCount, int stackCount)
        {
            List<ushort> indices = new List<ushort>();
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
                        indices.Add((ushort)k1);
                        indices.Add((ushort)k2);
                        indices.Add((ushort)(k1 + 1));
                    }

                    // k1+1 => k2 => k2+1
                    if (i != (stackCount - 1))
                    {
                        indices.Add((ushort)(k1 + 1));
                        indices.Add((ushort)k2);
                        indices.Add((ushort)(k2 + 1));
                    }
                }
            }

            return indices.ToArray();
        }
    }
}

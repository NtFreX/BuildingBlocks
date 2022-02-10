using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using BepuBufferPool = BepuUtilities.Memory.BufferPool;

namespace NtFreX.BuildingBlocks.Models
{
    public static class PlaneModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, Index16> CreateMesh(
           float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f,
           int rows = 2, int columns = 2, MaterialInfo? material = null)
        {
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows), "Rows need to be bigger then 1");
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(rows), "Columns need to be bigger then 1");

            var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), rows, columns);
            var indices = GetIndices(rows, columns);
            return new MeshDataProvider<VertexPositionColorNormalTexture, Index16>(vertices, indices, PrimitiveTopology.TriangleList, material: material);
        }

        public static Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders,
            ModelCreationInfo? creationInfo = null,
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, int rows = 2, int columns = 2,
            TextureView? texture = null, MaterialInfo? material = null, string? name = null,
            DeviceBufferPool? deviceBufferPool = null, BepuBufferPool? physicsBufferPool = null)
        {
            var mesh = CreateMesh(red, green, blue, alpha, rows, columns, material);
            if(physicsBufferPool == null)
                return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh, creationInfo: creationInfo, textureView: texture, name: name, deviceBufferPool: deviceBufferPool);

            var shape = mesh.GetPhysicsMesh(physicsBufferPool, creationInfo != null ? creationInfo.Value.Scale : Vector3.One);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh, shape, creationInfo: creationInfo, textureView: texture, name: name, deviceBufferPool: deviceBufferPool);
        }

        private static VertexPositionColorNormalTexture[] GetVertices(RgbaFloat color, int rows, int columns)
        {
            var vertices = new List<VertexPositionColorNormalTexture>();
            var halfRows = -(rows / 2f);
            var halfColumns = -(columns / 2f);
            for (float i = 0; i < rows; i++)
            {
                for (float j = 0; j < columns; j++)
                {
                    vertices.Add(new VertexPositionColorNormalTexture(new Vector3(i + halfRows, 0, j + halfColumns), color, new Vector2(i + i % 1 - rows, j + j % 1 - columns)));
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
                    indices.Add(one);
                    indices.Add(one + columns + 1);
                    indices.Add(one + 1);

                    indices.Add(one + columns);
                    indices.Add(one + columns + 1);
                    indices.Add(one);
                }
            }
            return indices.ToArray();
        }
    }
}

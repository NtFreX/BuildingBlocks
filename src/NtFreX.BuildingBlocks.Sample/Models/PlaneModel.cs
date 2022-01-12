using BepuPhysics;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    static class PlaneModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, ushort> CreateMesh(
           float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f,
           int rows = 2, int columns = 2, MaterialInfo? material = null)
        {
            if (rows < 2)
                throw new ArgumentOutOfRangeException(nameof(rows), "Rows need to be bigger then 1");
            if (columns < 2)
                throw new ArgumentOutOfRangeException(nameof(rows), "Columns need to be bigger then 1");

            var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), rows, columns);
            var indices = GetIndices(rows, columns);
            return new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(
                vertices, indices, IndexFormat.UInt16, PrimitiveTopology.TriangleList,
                VertexPositionColorNormalTexture.VertexLayout, material: material,
                bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition);
        }

        public static Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem,
            ModelCreationInfo creationInfo, Shader[] shaders,
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f,
            int rows = 2, int columns = 2,
            TextureView? texture = null, MaterialInfo? material = null, string? name = null)
        {
            var mesh = CreateMesh(red, green, blue, alpha, rows, columns, material);
            var shapeAllocator = (Simulation simulation) => mesh.GetPhysicsMesh(simulation, creationInfo.Scale);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders, mesh, shapeAllocator, textureView: texture, name: name);
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

        private static ushort[] GetIndices(int rows, int columns)
        {
            var indices = new List<ushort>();
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    var one = columns * i + j;
                    indices.Add((ushort)one);
                    indices.Add((ushort)(one + columns + 1));
                    indices.Add((ushort)(one + 1));

                    indices.Add((ushort)(one + columns));
                    indices.Add((ushort)(one + columns + 1));
                    indices.Add((ushort)one);
                }
            }
            return indices.ToArray();
        }
    }
}

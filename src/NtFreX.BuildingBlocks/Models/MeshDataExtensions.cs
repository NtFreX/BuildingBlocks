using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public static class MeshDataExtensions
    {
        public static Mesh GetPhysicsMesh(this MeshData meshData, Simulation simulation, Vector3 scale)
        {
            var triangles = meshData.GetTriangles();
            simulation.BufferPool.Take<Triangle>(triangles.Length, out var buffer);
            for (int i = 0; i < triangles.Length; ++i)
            {
                ref var triangle1 = ref buffer[i];
                triangle1.A = triangles[i].A;
                triangle1.B = triangles[i].B;
                triangle1.C = triangles[i].C;
            }

            return new Mesh(buffer, scale, simulation.BufferPool);
        }

        public static Triangle[] GetTriangles(this MeshData mesh)
            => mesh is MeshDataProvider dataProvider 
                ? GetTriangles(dataProvider) 
                : mesh.GetTriangles(mesh.GetIndices().Select(x => Convert.ToUInt32(x)).ToArray());
        public static Triangle[] GetTriangles(this MeshDataProvider mesh)
            => mesh.GetTriangles(mesh.IndexFormat == IndexFormat.UInt32 ? mesh.GetIndices32Bit() : mesh.GetIndices().Select(x => Convert.ToUInt32(x)).ToArray());
        private static Triangle[] GetTriangles(this MeshData mesh, uint[] indexes)
        {
            var points = mesh.GetVertexPositions();
            var triangles = new Triangle[indexes.Length / 3];
            for (var i = 0; i < indexes.Length; i += 3)
            {
                triangles[i / 3] = new Triangle(points[indexes[i]], points[indexes[i + 1]], points[indexes[i + 2]]);
            }
            return triangles;
        }

        public static (DeviceBuffer VertexBuffer, DeviceBuffer IndexBuffer, int IndexCount) BuildVertexAndIndexBuffer(this MeshData mesh, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            var commandListDescription = new CommandListDescription();
            var commandList = resourceFactory.CreateCommandList(ref commandListDescription);
            commandList.Begin();

            var vertexBuffer = mesh.CreateVertexBuffer(resourceFactory, commandList);
            var indexBuffer = mesh.CreateIndexBuffer(resourceFactory, commandList, out var indexCount);

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            commandList.Dispose();

            return (vertexBuffer, indexBuffer, indexCount);
        }
    }
}

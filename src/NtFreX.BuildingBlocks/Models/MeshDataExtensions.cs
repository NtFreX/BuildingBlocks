using BepuPhysics.Collidables;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public static class MeshDataExtensions
    {
        public static Triangle[] GetTriangles(this MeshData mesh)
        {
            var points = mesh.GetVertexPositions();
            var indexes = mesh.GetIndices();
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

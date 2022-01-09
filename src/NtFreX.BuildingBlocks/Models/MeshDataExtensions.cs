using BepuPhysics.Collidables;
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
    }
}

using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;
using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;
using BepuBufferPool = BepuUtilities.Memory.BufferPool;

namespace NtFreX.BuildingBlocks.Mesh
{
    public static class MeshDataExtensions
    {
        public static BepuPhysicsMesh GetPhysicsMesh(this MeshData meshData, BepuBufferPool bufferPool)
            => GetPhysicsMesh(meshData, bufferPool, Vector3.One);
        public static BepuPhysicsMesh GetPhysicsMesh(this MeshData meshData, BepuBufferPool bufferPool, Vector3 scale)
        {
            var triangles = meshData.GetTriangles();
            bufferPool.Take<Triangle>(triangles.Length, out var buffer);
            for (int i = 0; i < triangles.Length; ++i)
            {
                ref var triangle1 = ref buffer[i];
                triangle1.A = triangles[i].A;
                triangle1.B = triangles[i].B;
                triangle1.C = triangles[i].C;
            }

            return new BepuPhysicsMesh(buffer, scale, bufferPool);
        }

        public static Triangle[] GetTriangles(this MeshData mesh)
            => mesh is MeshDataProvider dataProvider 
                ? GetTriangles(dataProvider) 
                : mesh.GetTriangles(mesh.GetIndices().Select(x => (Index32)x).ToArray());
        public static Triangle[] GetTriangles(this MeshDataProvider mesh)
        {
            if (mesh.PrimitiveTopology != PrimitiveTopology.TriangleList)
                throw new Exception("Only meshes with triangle lists are supported");

            return mesh.GetTriangles(mesh.IndexFormat == IndexFormat.UInt32 ? mesh.GetIndices32Bit() : mesh.GetIndices().Select(x => (Index32)x).ToArray());
        }
        private static Triangle[] GetTriangles(this MeshData mesh, Index32[] indexes)
        {
            var points = mesh.GetVertexPositions();
            var triangles = new Triangle[indexes.Length / 3];
            for (var i = 0; i < indexes.Length; i += 3)
            {
                triangles[i / 3] = new Triangle(points[indexes[i]], points[indexes[i + 1]], points[indexes[i + 2]]);
            }
            return triangles;
        }

        public static MeshDataProvider<VertexPosition, Index32> CombineVertexPosition32Bit(this IEnumerable<MeshDataProvider> meshDataProviders)
        {
            var indices = new List<Index32>();
            var vertices = new List<VertexPosition>();
            foreach (var data in meshDataProviders)
            {
                if (data.PrimitiveTopology != PrimitiveTopology.TriangleList)
                    throw new ArgumentException("Only triangle lists are suported");

                indices.AddRange(data.GetIndices32Bit().Select(index => (Index32)(index + vertices.Count)));
                vertices.AddRange(data.GetVertexPositions().Select(position => new VertexPosition(position)));
            }

            return new MeshDataProvider<VertexPosition, Index32>(vertices.ToArray(), indices.ToArray(), PrimitiveTopology.TriangleList);
        }

        // TODO: remove doublicated vertices
        //public static MeshDeviceBuffer<TVertex, TIndex> Simplify()

        public static MeshDataProvider<TVertex, TIndex> Combine<TVertex, TIndex>(this IEnumerable<MeshDataProvider<TVertex, TIndex>> meshDataProviders)
            where TVertex : unmanaged, IVertex
            where TIndex : unmanaged, IIndex<TIndex>
        {
            var indices = new List<TIndex>();
            var vertices = new List<TVertex>();
            string? materialName = null;
            MaterialInfo? material = null;
            string? texture = null;
            foreach (var data in meshDataProviders)
            {
                if (data.PrimitiveTopology != PrimitiveTopology.TriangleList)
                    throw new ArgumentException("Only triangle lists are suported");

                indices.AddRange(data.Indices.Select(index => TIndex.ParseInt((uint) (index.AsUInt() + vertices.Count))));
                vertices.AddRange(data.Vertices);

                // TODO: validate that they are the same?
                materialName = data.MaterialName;
                material = data.Material;
                texture = data.TexturePath;
            }

            return new MeshDataProvider<TVertex, TIndex>(vertices.ToArray(), indices.ToArray(), PrimitiveTopology.TriangleList, materialName: materialName, texturePath: texture, material: material);
        }

        public static MeshDataProvider<TVertex, TIndex> Define<TVertex, TIndex>(this BinaryMeshDataProvider binaryMesh, Func<byte[], TVertex> buildVertex)
            where TVertex : unmanaged, IVertex
            where TIndex : unmanaged, IIndex<TIndex>
        {
            var indexCount = binaryMesh.GetIndexCount();
            List<TVertex> vertices = new List<TVertex>();
            var indices = new TIndex[indexCount];
            for (var index = 0; index < indexCount; index++)
            {
                var vertexData = buildVertex(binaryMesh.GetVertexData(index));
                var realIndex = vertices.IndexOf(vertexData);

                if (realIndex < 0)
                {
                    indices[index] = TIndex.ParseInt((uint)vertices.Count);
                    vertices.Add(vertexData);
                }
                else
                {
                    indices[index] = TIndex.ParseInt((uint)realIndex);
                }
            }

            return new MeshDataProvider<TVertex, TIndex>(vertices.ToArray(), indices, binaryMesh.PrimitiveTopology, binaryMesh.MaterialName, binaryMesh.TexturePath, binaryMesh.Material);
        }

        public static MeshDataProvider<TVertex, Index16> MutateTo16BitIndex<TVertex>(this MeshDataProvider<TVertex, Index32> meshDataProvider)
            where TVertex : unmanaged, IVertex
            => meshDataProvider.MutateIndices(x => Index16.Parse(x));

        public static MeshDataProvider<TVertex, Index32> MutateTo32BitIndex<TVertex>(this MeshDataProvider<TVertex, Index16> meshDataProvider)
            where TVertex : unmanaged, IVertex
            => meshDataProvider.MutateIndices(x => Index32.Parse(x));

        public static MeshDataProvider<TVertex, TIndexOut> MutateIndices<TVertex, TIndexIn, TIndexOut>(this MeshDataProvider<TVertex, TIndexIn> meshDataProvider, Func<TIndexIn, TIndexOut> mutateIndexFnc)
            where TVertex : unmanaged, IVertex
            where TIndexIn : unmanaged, IIndex
            where TIndexOut : unmanaged, IIndex
            => meshDataProvider.Mutate(x => x, mutateIndexFnc);

        public static MeshDataProvider<TVertexOut, TIndex> MutateVertices<TVertexIn, TIndex, TVertexOut>(this MeshDataProvider<TVertexIn, TIndex> meshDataProvider, Func<TVertexIn, TVertexOut> mutateVertexFnc)
            where TVertexIn : unmanaged, IVertex
            where TIndex : unmanaged, IIndex
            where TVertexOut : unmanaged, IVertex
            => meshDataProvider.Mutate(mutateVertexFnc, x => x);

        public static MeshDataProvider<TVertexOut, TIndexOut> Mutate<TVertexIn, TIndexIn, TVertexOut, TIndexOut>(this MeshDataProvider<TVertexIn, TIndexIn> meshDataProvider, Func<TVertexIn, TVertexOut> mutateVertexFnc, Func<TIndexIn, TIndexOut> mutateIndexFnc)
            where TVertexIn : unmanaged, IVertex
            where TIndexIn : unmanaged, IIndex
            where TVertexOut : unmanaged, IVertex
            where TIndexOut : unmanaged, IIndex
            => new MeshDataProvider<TVertexOut, TIndexOut>(
                meshDataProvider.Vertices.Select(mutateVertexFnc).ToArray(), 
                meshDataProvider.Indices.Select(mutateIndexFnc).ToArray(), 
                meshDataProvider.PrimitiveTopology, meshDataProvider.MaterialName, meshDataProvider.TexturePath, meshDataProvider.Material); 

        public static (PooledDeviceBuffer VertexBuffer, PooledDeviceBuffer IndexBuffer, int IndexCount) BuildVertexAndIndexBuffer(this MeshDataProvider mesh, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        {
            var commandList = CommandListPool.TryGet(resourceFactory, commandListPool);
            var vertexBuffer = mesh.CreateVertexBuffer(resourceFactory, commandList.Item, deviceBufferPool);
            var indexBuffer = mesh.CreateIndexBuffer(resourceFactory, commandList.Item, out var indexCount, deviceBufferPool);
            CommandListPool.TryClean(graphicsDevice, commandList, commandListPool);

            return (vertexBuffer, indexBuffer, indexCount);
        }
    }
}

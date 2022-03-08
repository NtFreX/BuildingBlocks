using BepuPhysics.Collidables;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;

using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;
using BepuBufferPool = BepuUtilities.Memory.BufferPool;
using VertexPosition = NtFreX.BuildingBlocks.Mesh.Primitives.VertexPosition;

namespace NtFreX.BuildingBlocks.Mesh.Data
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
            => mesh is SpecializedMeshData dataProvider 
                ? GetTriangles(dataProvider) 
                : mesh.GetTriangles(mesh.GetIndices().Select(x => (Index32)x).ToArray());
        public static Triangle[] GetTriangles(this SpecializedMeshData mesh)
        {
            if (mesh.DrawConfiguration.PrimitiveTopology != PrimitiveTopology.TriangleList)
                throw new Exception("Only meshes with triangle lists are supported");

            return mesh.GetTriangles(mesh.DrawConfiguration.IndexFormat == IndexFormat.UInt32 ? mesh.GetIndices32Bit() : mesh.GetIndices().Select(x => (Index32)x).ToArray());
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

        public static DefinedMeshData<VertexPosition, Index32> CombineVertexPosition32Bit(this IEnumerable<SpecializedMeshData> meshDataProviders)
        {
            var indices = new List<Index32>();
            var vertices = new List<VertexPosition>();
            MeshDataSpecializationDictionary? specializations = default;
            DrawConfiguration? drawConfiguration = default;
            foreach (var data in meshDataProviders)
            {
                if (data.DrawConfiguration.PrimitiveTopology != PrimitiveTopology.TriangleList)
                    throw new ArgumentException("Only triangle lists are suported");

                indices.AddRange(data.GetIndices32Bit().Select(index => (Index32)(index + vertices.Count)));
                vertices.AddRange(data.GetVertexPositions().Select(position => new VertexPosition(position)));

                // TODO: validate that they are the same?
                specializations = data.Specializations;
                drawConfiguration = data.DrawConfiguration;
            }

            if (specializations == null || drawConfiguration == null)
                throw new ArgumentException($"Empty enumerables are not supported");

            return new DefinedMeshData<VertexPosition, Index32>(vertices.ToArray(), indices.ToArray(), drawConfiguration.PrimitiveTopology, drawConfiguration.FillMode, drawConfiguration.FaceCullMode, specializations);
        }

        // TODO: remove doublicated vertices
        //public static MeshDeviceBuffer<TVertex, TIndex> Simplify()

        public static DefinedMeshData<TVertex, TIndex> Combine<TVertex, TIndex>(this IEnumerable<DefinedMeshData<TVertex, TIndex>> meshDataProviders)
            where TVertex : unmanaged, IVertex
            where TIndex : unmanaged, IIndex<TIndex>
        {
            var indices = new List<TIndex>();
            var vertices = new List<TVertex>();
            MeshDataSpecializationDictionary? specializations = default;
            DrawConfiguration? drawConfiguration = default;
            foreach (var data in meshDataProviders)
            {
                if (data.DrawConfiguration.PrimitiveTopology != PrimitiveTopology.TriangleList)
                    throw new ArgumentException("Only triangle lists are suported");

                indices.AddRange(data.Indices.Select(index => TIndex.ParseInt((uint) (index.AsUInt() + vertices.Count))));
                vertices.AddRange(data.Vertices);

                // TODO: validate that they are the same?
                specializations = data.Specializations;
                drawConfiguration = data.DrawConfiguration;
            }

            if (specializations == null || drawConfiguration == null)
                throw new ArgumentException($"Empty enumerables are not supported");

            return new DefinedMeshData<TVertex, TIndex>(vertices.ToArray(), indices.ToArray(), drawConfiguration.PrimitiveTopology, drawConfiguration.FillMode, drawConfiguration.FaceCullMode, specializations);
        }

        public static DefinedMeshData<TVertex, TIndex> Define<TVertex, TIndex>(this BinaryMeshData binaryMesh, Func<byte[], TVertex> buildVertex)
            where TVertex : unmanaged, IVertex
            where TIndex : unmanaged, IIndex<TIndex>
        {
            var indexCount = binaryMesh.GetIndexCount();
            var vertices = new List<TVertex>();
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

            return new DefinedMeshData<TVertex, TIndex>(vertices.ToArray(), indices, binaryMesh.DrawConfiguration.PrimitiveTopology, binaryMesh.DrawConfiguration.FillMode, binaryMesh.DrawConfiguration.FaceCullMode, binaryMesh.Specializations);
        }

        public static DefinedMeshData<TVertex, Index16> MutateTo16BitIndex<TVertex>(this DefinedMeshData<TVertex, Index32> meshDataProvider)
            where TVertex : unmanaged, IVertex
            => meshDataProvider.MutateIndices(x => Index16.Parse(x));

        public static DefinedMeshData<TVertex, Index32> MutateTo32BitIndex<TVertex>(this DefinedMeshData<TVertex, Index16> meshDataProvider)
            where TVertex : unmanaged, IVertex
            => meshDataProvider.MutateIndices(x => Index32.Parse(x));

        public static DefinedMeshData<TVertex, TIndexOut> MutateIndices<TVertex, TIndexIn, TIndexOut>(this DefinedMeshData<TVertex, TIndexIn> meshDataProvider, Func<TIndexIn, TIndexOut> mutateIndexFnc)
            where TVertex : unmanaged, IVertex
            where TIndexIn : unmanaged, IIndex
            where TIndexOut : unmanaged, IIndex
            => meshDataProvider.Mutate(x => x, mutateIndexFnc);

        public static DefinedMeshData<TVertexOut, TIndex> MutateVertices<TVertexIn, TIndex, TVertexOut>(this DefinedMeshData<TVertexIn, TIndex> meshDataProvider, Func<TVertexIn, TVertexOut> mutateVertexFnc)
            where TVertexIn : unmanaged, IVertex
            where TIndex : unmanaged, IIndex
            where TVertexOut : unmanaged, IVertex
            => meshDataProvider.Mutate(mutateVertexFnc, x => x);

        public static DefinedMeshData<TVertexOut, TIndexOut> Mutate<TVertexIn, TIndexIn, TVertexOut, TIndexOut>(this DefinedMeshData<TVertexIn, TIndexIn> meshDataProvider, Func<TVertexIn, TVertexOut> mutateVertexFnc, Func<TIndexIn, TIndexOut> mutateIndexFnc)
            where TVertexIn : unmanaged, IVertex
            where TIndexIn : unmanaged, IIndex
            where TVertexOut : unmanaged, IVertex
            where TIndexOut : unmanaged, IIndex
            => new (
                meshDataProvider.Vertices.Select(mutateVertexFnc).ToArray(),
                meshDataProvider.Indices.Select(mutateIndexFnc).ToArray(),
                meshDataProvider.DrawConfiguration.PrimitiveTopology, meshDataProvider.DrawConfiguration.FillMode, meshDataProvider.DrawConfiguration.FaceCullMode, meshDataProvider.Specializations);

        public static (PooledDeviceBuffer VertexBuffer, PooledDeviceBuffer IndexBuffer, uint IndexCount) BuildVertexAndIndexBuffer(this SpecializedMeshData mesh, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        {
            var commandList = CommandListPool.TryGet(resourceFactory, commandListPool: commandListPool);
            var vertexBuffer = mesh.CreateVertexBuffer(resourceFactory, commandList.CommandList, deviceBufferPool);
            var indexBuffer = mesh.CreateIndexBuffer(resourceFactory, commandList.CommandList, out var indexCount, deviceBufferPool);
            CommandListPool.TrySubmit(graphicsDevice, commandList, commandListPool);

            return (vertexBuffer, indexBuffer, (uint)indexCount);
        }
    }
}

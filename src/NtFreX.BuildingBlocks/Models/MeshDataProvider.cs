using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    // TODO: use as base for collidables
    //public class TriangleMeshDataProvider<TVertex, TIndex> : MeshData
    //{

    //}

    public class MeshDataProvider<TVertex, TIndex> : MeshData
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        private readonly int VertexSize = Marshal.SizeOf(typeof(TVertex));
        private readonly Func<TVertex, Vector3> positionAccessor;
        private readonly int bytesBeforePosition;

        public TVertex[] Vertices { get; }
        public TIndex[] Indices { get; }
        public IndexFormat IndexFormat { get; }
        public string? MaterialName { get; }


        public MeshDataProvider(TVertex[] vertices, TIndex[] indices, Func<TVertex, Vector3> positionAccessor, IndexFormat indexFormat)
            : this(vertices, indices, positionAccessor, indexFormat, null) { }

        public MeshDataProvider(TVertex[] vertices, TIndex[] indices, Func<TVertex, Vector3> positionAccessor, IndexFormat indexFormat, string? materialName, int bytesBeforePosition = 0)
        {
            var indexType = typeof(TIndex);
            if (!(indexType == typeof(ushort) || indexType == typeof(uint)))
            {
                throw new ArgumentException($"The type {indexType.FullName} must either be ushort or uint");
            }

            Vertices = vertices;
            Indices = indices;
            IndexFormat = indexFormat;
            MaterialName = materialName;

            this.bytesBeforePosition = bytesBeforePosition;
            this.positionAccessor = positionAccessor;
        }

        public DeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList commandList)
        {
            DeviceBuffer vb = factory.CreateBuffer(new BufferDescription((uint)(Vertices.Length * VertexSize), BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vb, 0, Vertices);
            return vb;
        }

        public DeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList commandList, out int indexCount)
        {
            DeviceBuffer ib = factory.CreateBuffer(new BufferDescription((uint)(Indices.Length * Marshal.SizeOf(typeof(TIndex))), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(ib, 0, Indices);
            indexCount = Indices.Length;
            return ib;
        }

        public unsafe BoundingSphere GetBoundingSphere()
        {
            fixed (TVertex* ptr = Vertices)
            {
                return BoundingSphere.CreateFromPoints((Vector3*)(ptr + bytesBeforePosition), Vertices.Length, VertexSize);
            }
        }

        public unsafe BoundingBox GetBoundingBox()
        {
            fixed (TVertex* ptr = Vertices)
            {
                return BoundingBox.CreateFromPoints(
                    (Vector3*)(ptr + bytesBeforePosition),
                    Vertices.Length,
                    VertexSize,
                    Quaternion.Identity,
                    Vector3.Zero,
                    Vector3.One);
            }

        }

        public bool RayCast(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            bool result = false;
            for (int i = 0; i < Indices.Length - 2; i += 3)
            {
                Vector3 v0 = positionAccessor(Vertices[(dynamic)Indices[i + 0]]);
                Vector3 v1 = positionAccessor(Vertices[(dynamic)Indices[i + 1]]);
                Vector3 v2 = positionAccessor(Vertices[(dynamic)Indices[i + 2]]);

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                    }

                    result = true;
                }
            }

            return result;
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            int hits = 0;
            for (int i = 0; i < Indices.Length - 2; i += 3)
            {
                Vector3 v0 = positionAccessor(Vertices[(dynamic)Indices[i + 0]]);
                Vector3 v1 = positionAccessor(Vertices[(dynamic)Indices[i + 1]]);
                Vector3 v2 = positionAccessor(Vertices[(dynamic)Indices[i + 2]]);

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    hits++;
                    distances.Add(newDistance);
                }
            }

            return hits;
        }

        public Vector3[] GetVertexPositions()
        {
            return Vertices.Select(vpnt => positionAccessor(vpnt)).ToArray();
        }

        public IndexFormat GetIndexFormat()
        {
            return IndexFormat;
        }


        public int GetIndexCount() => Indices.Length;
        
        public int GetVertexCount() => Vertices.Length;

        public ushort[] GetIndices()
        {
            return GetIndices16Bit();
        }

        public ushort[] GetIndices16Bit()
        {
            if (IndexFormat == IndexFormat.UInt32)
                throw new NotSupportedException();

            return Indices.Cast<ushort>().ToArray();
        }

        public uint[] GetIndices32Bit()
        {
            if (IndexFormat == IndexFormat.UInt16)
                throw new NotSupportedException();

            return Indices.Cast<uint>().ToArray();
        }
    }
}

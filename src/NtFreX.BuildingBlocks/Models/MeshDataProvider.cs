using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public abstract class MeshDataProvider : MeshData
    {
        public IndexFormat IndexFormat { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; }
        public VertexLayoutDescription VertexLayout { get; set; }
        public string? MaterialName { get; set; }
        public string? TexturePath { get; set; }

        public MaterialInfo Material { get; set; }

        public abstract DeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList cl, out int indexCount);
        public abstract DeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList cl);
        public abstract BoundingBox GetBoundingBox();
        public abstract BoundingSphere GetBoundingSphere();
        public abstract ushort[] GetIndices();
        public abstract ushort[] GetIndices16Bit();
        public abstract uint[] GetIndices32Bit();
        public abstract Vector3[] GetVertexPositions();
        public abstract bool RayCast(Ray ray, out float distance);
        public abstract int RayCast(Ray ray, List<float> distances);
    }
    public class MeshDataProvider<TVertex, TIndex> : MeshDataProvider
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        private readonly int VertexSize = Marshal.SizeOf(typeof(TVertex));
        private readonly int bytesBeforePosition;

        public TVertex[] Vertices { get; set; }
        public TIndex[] Indices { get; set; }

        public MeshDataProvider(TVertex[] vertices, TIndex[] indices, IndexFormat indexFormat, PrimitiveTopology primitiveTopology, VertexLayoutDescription vertexLayout, string? materialName = null, string? texturePath = null, int bytesBeforePosition = 0, MaterialInfo? material = null)
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
            Material = material ?? new MaterialInfo();
            PrimitiveTopology = primitiveTopology;
            TexturePath = texturePath;
            VertexLayout = vertexLayout;

            this.bytesBeforePosition = bytesBeforePosition;
        }

        public override DeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList commandList)
        {
            DeviceBuffer vb = factory.CreateBuffer(new BufferDescription((uint)(Vertices.Length * VertexSize), BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vb, 0, Vertices);
            return vb;
        }

        public override DeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList commandList, out int indexCount)
        {
            DeviceBuffer ib = factory.CreateBuffer(new BufferDescription((uint)(Indices.Length * Marshal.SizeOf(typeof(TIndex))), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(ib, 0, Indices);
            indexCount = Indices.Length;
            return ib;
        }

        public override unsafe BoundingSphere GetBoundingSphere()
        {
            fixed (TVertex* ptr = Vertices)
            {
                return BoundingSphere.CreateFromPoints((Vector3*)(ptr + bytesBeforePosition), Vertices.Length, VertexSize);
            }
        }

        public override unsafe BoundingBox GetBoundingBox()
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

        public override unsafe bool RayCast(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            bool result = false;

            fixed (TVertex* ptr = Vertices)
            {
                for (int i = 0; i < Indices.Length - 2; i += 3)
                {
                    var v0 = GetVertexPositionAt((int)(object)Indices[i + 0]);
                    var v1 = GetVertexPositionAt((int)(object)Indices[i + 1]);
                    var v2 = GetVertexPositionAt((int)(object)Indices[i + 2]);

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
            }
            return result;
        }

        public override int RayCast(Ray ray, List<float> distances)
        {
            int hits = 0;
            for (int i = 0; i < Indices.Length - 2; i += 3)
            {
                var v0 = GetVertexPositionAt((int)(object)Indices[i + 0]);
                var v1 = GetVertexPositionAt((int)(object)Indices[i + 1]);
                var v2 = GetVertexPositionAt((int)(object)Indices[i + 2]);

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    hits++;
                    distances.Add(newDistance);
                }
            }

            return hits;
        }
        
        private unsafe Vector3 GetVertexPositionAt(int index)
        {
            fixed (TVertex* ptr = Vertices)
            {
                var vertexPtr = ptr + index;
                var posPtr = (byte*)vertexPtr + bytesBeforePosition;

                return *(Vector3*)posPtr;
            }
        }

        public override Vector3[] GetVertexPositions()
        {
            return Vertices.Select((v, i) => GetVertexPositionAt(i)).ToArray();
        }

        public IndexFormat GetIndexFormat()
        {
            return IndexFormat;
        }


        public int GetIndexCount() => Indices.Length;
        
        public int GetVertexCount() => Vertices.Length;

        public override ushort[] GetIndices()
        {
            checked
            {
                return IndexFormat == IndexFormat.UInt16
                    ? GetIndices16Bit()
                    : GetIndices32Bit().Select(x => Convert.ToUInt16(x)).ToArray();
            }
        }

        public override ushort[] GetIndices16Bit()
        {
            if (IndexFormat == IndexFormat.UInt32)
                throw new NotSupportedException();

            return Indices.Cast<ushort>().ToArray();
        }

        public override uint[] GetIndices32Bit()
        {
            if (IndexFormat == IndexFormat.UInt16)
                throw new NotSupportedException();

            return Indices.Cast<uint>().ToArray();
        }
    }
}

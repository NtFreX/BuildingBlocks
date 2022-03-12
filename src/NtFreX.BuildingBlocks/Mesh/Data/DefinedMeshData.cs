using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using ProtoBuf;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data
{
    public class DefinedMeshData<TVertex, TIndex> : SpecializedMeshData, IEquatable<DefinedMeshData<TVertex, TIndex>>, IProtobufSerializable<DefinedMeshData<TVertex, TIndex>.Protobuf, DefinedMeshData<TVertex, TIndex>>
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, IIndex
    {
        [ProtoContract]
        public class Protobuf
        {
            [ProtoMember(400)] public DrawConfiguration.Protobuf DrawConfiguration;
            //[ProtoMember(1)] public MeshDataSpecializationDictionaryProtobuf Specializations { get; init; } //TODO: serialize specializations
            [ProtoMember(402)] public byte[] Vertices;
            [ProtoMember(403)] public byte[] Indices;
        }


        public Protobuf ToSerializable() => new Protobuf { DrawConfiguration = DrawConfiguration.ToSerializable(), Vertices = BitConverterExtensions.ArrayToBytes(Vertices), Indices = BitConverterExtensions.ArrayToBytes(Indices) };
        public static DefinedMeshData<TVertex, TIndex> FromSerializable(Protobuf data) => new DefinedMeshData<TVertex, TIndex>(
            BitConverterExtensions.ArrayFromBytes<TVertex>(data.Vertices), BitConverterExtensions.ArrayFromBytes<TIndex>(data.Indices), 
            data.DrawConfiguration.PrimitiveTopology, data.DrawConfiguration.FillMode, data.DrawConfiguration.FaceCullMode, null /* TODO: specializations */);


        private readonly int VertexSize = Marshal.SizeOf(typeof(TVertex));

        private TVertex[] vertices;
        private TIndex[] indices;

        public TVertex[] Vertices { get => vertices; set { vertices = value; HasVertexChanges = true; } }
        public TIndex[] Indices { get => indices; set { indices = value; HasIndexChanges = true; } }

        public DefinedMeshData(
            TVertex[] vertices, TIndex[] indices, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, 
            PolygonFillMode polygonFillMode = PolygonFillMode.Solid, FaceCullMode faceCullMode = FaceCullMode.None, MeshDataSpecializationDictionary? meshDataSpecializations = null)
            : base(new DrawConfiguration(TIndex.IndexFormat, primitiveTopology, TVertex.VertexLayout, polygonFillMode, faceCullMode), meshDataSpecializations ?? new ())
        {
            Vertices = vertices;
            Indices = indices;
        }

        public override PooledDeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList commandList, DeviceBufferPool? deviceBufferPool = null)
        {
            var desc = new BufferDescription((uint)(Vertices.Length * VertexSize), BufferUsage.VertexBuffer);
            var vb = factory.CreatedPooledBuffer(desc, GetType().Name + "_vertexbuffer", deviceBufferPool);
            commandList.UpdateBuffer(vb.RealDeviceBuffer, 0, Vertices);
            return vb;
        }

        public override void UpdateVertexBuffer(CommandList commandList, PooledDeviceBuffer buffer)
        {
            base.UpdateVertexBuffer(commandList, buffer);
            commandList.UpdateBuffer(buffer.RealDeviceBuffer, 0, Vertices);
        }

        public override PooledDeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList commandList, out int indexCount, DeviceBufferPool? deviceBufferPool = null)
        {
            var desc = new BufferDescription((uint)(Indices.Length * Marshal.SizeOf(typeof(TIndex))), BufferUsage.IndexBuffer);
            var ib = factory.CreatedPooledBuffer(desc, GetType().Name + "_indexbuffer", deviceBufferPool);
            commandList.UpdateBuffer(ib.RealDeviceBuffer, 0, Indices);
            indexCount = Indices.Length;
            return ib;
        }

        public override uint UpdateIndexBuffer(CommandList commandList, PooledDeviceBuffer buffer)
        {
            base.UpdateIndexBuffer(commandList, buffer);
            commandList.UpdateBuffer(buffer.RealDeviceBuffer, 0, Indices);
            return (uint)indices.Length;
        }

        public override unsafe BoundingSphere GetBoundingSphere()
        {
            fixed (TVertex* ptr = Vertices)
            {
                return BoundingSphere.CreateFromPoints((Vector3*)(ptr + TVertex.BytesBeforePosition), Vertices.Length, VertexSize);
            }
        }

        public override unsafe BoundingBox GetBoundingBox()
        {
            fixed (TVertex* ptr = Vertices)
            {
                return BoundingBox.CreateFromPoints(
                    (Vector3*)(ptr + TVertex.BytesBeforePosition),
                    Vertices.Length,
                    VertexSize,
                    Quaternion.Identity,
                    Vector3.Zero,
                    Vector3.One);
            }
        }

        public override uint GetIndexPositionAt(int index)
            => (uint)(object)Indices[index];

        public override unsafe Vector3 GetVertexPositionAt(uint index)
        {
            fixed (TVertex* ptr = Vertices)
            {
                var vertexPtr = ptr + index;
                var posPtr = (byte*)vertexPtr + TVertex.BytesBeforePosition;

                return *(Vector3*)posPtr;
            }
        }

        public unsafe override Vector3[] GetVertexPositions()
        {
            var vertextPositions = new Vector3[Vertices.Length];
            fixed (TVertex* ptr = Vertices)
            {
                for (var index = 0; index < Vertices.Length; index++)
                {
                    var vertexPtr = ptr + index;
                    var posPtr = (byte*)vertexPtr + TVertex.BytesBeforePosition;

                    vertextPositions[index] = *(Vector3*)posPtr;
                }
            }
            return vertextPositions;
        }

        public override int GetIndexCount() 
            => Indices.Length;
        public override int GetVertexCount()
            => Vertices.Length;

        public override ushort[] GetIndices()
        {
            checked
            {
                return DrawConfiguration.IndexFormat == IndexFormat.UInt16
                    ? GetIndices16Bit().Select(x => x.Value).ToArray()
                    : GetIndices32Bit().Select(x => { checked { return (ushort)x.Value; } }).ToArray();
            }
        }

        public override Index16[] GetIndices16Bit()
        {
            if (DrawConfiguration.IndexFormat != IndexFormat.UInt16)
                throw new NotSupportedException();

            return Indices.Cast<Index16>().ToArray();
        }

        public override Index32[] GetIndices32Bit()
        {
            if (DrawConfiguration.IndexFormat != IndexFormat.UInt32)
                throw new NotSupportedException();

            return Indices.Cast<Index32>().ToArray();
        }

        public static bool operator !=(DefinedMeshData<TVertex, TIndex>? one, DefinedMeshData<TVertex, TIndex>? two)
            => !(one == two);

        public static bool operator ==(DefinedMeshData<TVertex, TIndex>? one, DefinedMeshData<TVertex, TIndex>? two)
            => EqualsExtensions.EqualsReferenceType(one, two);

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), (Vertices, Indices).GetHashCode());

        public override bool Equals([NotNullWhen(true)] object? obj)
            => EqualsExtensions.EqualsObject(this, obj);

        public bool Equals(DefinedMeshData<TVertex, TIndex>? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!base.Equals(other))
                return false;
            if (Vertices == null && other.Vertices != null ||
               Vertices != null && other.Vertices == null ||
               Vertices != null && other.Vertices != null && !other.Vertices.SequenceEqual(Vertices))
                return false;
            if (Indices == null && other.Indices != null ||
               Indices != null && other.Indices == null ||
               Indices != null && other.Indices != null && !other.Indices.SequenceEqual(Indices))
                return false;

            return true;
        }
    }
}

using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data
{
    public class BinaryMeshData : SpecializedMeshData
    {
        private readonly byte[] positionValues;
        private readonly byte[] normalValues;
        private readonly byte[] texCoordValues;
        private readonly byte[] colorValues;
        private readonly byte[] indices;

        private static unsafe Span<byte> GetSpan<T>(T[] data)
            where T: unmanaged
        {
            if (data == null || data.Length == 0)
                return new Span<byte>();

            fixed (T* ptr = &data[0])
            {
                return new Span<byte>((byte*)ptr, data.Length * Marshal.SizeOf<T>());
            }
        }

        public static unsafe BinaryMeshData Create(
            float[] positionValues, float[] normalValues, float[] texCoordValues, float[] colorValues, uint[] indexValues, VertexLayoutDescription vertexLayout,
            PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, PolygonFillMode polygonFillMode = PolygonFillMode.Solid, FaceCullMode faceCullMode = FaceCullMode.None, MeshDataSpecializationDictionary? meshDataSpecializations = null)
        {
            var positions = GetSpan(positionValues);
            var normals = GetSpan(normalValues);
            var texCoords = GetSpan(texCoordValues);
            var colors = GetSpan(colorValues);
            var indices = GetSpan(indexValues);
            return new BinaryMeshData(positions.ToArray(), normals.ToArray(), texCoords.ToArray(), colors.ToArray(), indices.ToArray(), vertexLayout, primitiveTopology, polygonFillMode, faceCullMode, meshDataSpecializations);
        }
        
        public BinaryMeshData(
            byte[] positionValues, byte[] normalValues, byte[] texCoordValues, byte[] colorValues, byte[] indices, VertexLayoutDescription vertexLayout,
            PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, PolygonFillMode polygonFillMode = PolygonFillMode.Solid, FaceCullMode faceCullMode = FaceCullMode.None, MeshDataSpecializationDictionary? meshDataSpecializations = null)
            : base(new DrawConfiguration(IndexFormat.UInt32, primitiveTopology, vertexLayout, polygonFillMode, faceCullMode), meshDataSpecializations ?? new())
        {
            this.positionValues = positionValues;
            this.normalValues = normalValues;
            this.texCoordValues = texCoordValues;
            this.colorValues = colorValues;
            this.indices = indices;
        }

        // TODO: do not doublicate the following methods!!!
        public override unsafe BoundingSphere GetBoundingSphere()
        {
            var vertexCount = GetVertexCount();
            var vertexSize = GetVertexSize();
            fixed (byte* ptr = &positionValues[0])
            {
                return BoundingSphere.CreateFromPoints((Vector3*)ptr, vertexCount, vertexSize);
            }
        }

        public override unsafe BoundingBox GetBoundingBox()
        {
            var vertexCount = GetVertexCount();
            var vertexSize = GetVertexSize();
            fixed (byte* ptr = &positionValues[0])
            {
                return BoundingBox.CreateFromPoints(
                    (Vector3*)ptr,
                    vertexCount,
                    vertexSize,
                    Quaternion.Identity,
                    Vector3.Zero,
                    Vector3.One);
            }

        }

        public override unsafe uint GetIndexPositionAt(int index)
        {
            fixed (byte* ptr = &indices[0])
            {
                var vertexPtr = ptr + index;

                return *(uint*)ptr;
            }
        }

        public override unsafe Vector3 GetVertexPositionAt(uint index)
        {
            fixed (byte* ptr = &positionValues[0])
            {
                var vertexPtr = ptr + index;

                return *(Vector3*)ptr;
            }
        }

        public unsafe override ushort[] GetIndices()
        {
            checked
            {
                return DrawConfiguration.IndexFormat == IndexFormat.UInt16
                    ? GetIndices16Bit().Select(x => x.Value).ToArray()
                    : GetIndices32Bit().Select(x => { checked { return (ushort)x.Value; } }).ToArray();
            }
        }

        public unsafe override Index16[] GetIndices16Bit()
        {
            if (DrawConfiguration.IndexFormat != IndexFormat.UInt16)
                throw new NotSupportedException();

            var indices16 = new Index16[GetIndexCount() * DrawConfiguration.VertexLayout.Elements.Length];
            fixed (Index16* ptr = &indices16[0])
            {
                Marshal.Copy(indices, 0, new IntPtr(ptr), indices.Length);
            }
            return indices16.Where((item, index) => index % DrawConfiguration.VertexLayout.Elements.Length == 0).ToArray();
        }

        public unsafe override Index32[] GetIndices32Bit()
        {
            if (DrawConfiguration.IndexFormat != IndexFormat.UInt32)
                throw new NotSupportedException();

            var indices32 = new Index32[GetIndexCount() * DrawConfiguration.VertexLayout.Elements.Length];
            fixed (Index32* ptr = &indices32[0])
            {
                Marshal.Copy(indices, 0, new IntPtr(ptr), indices.Length);
            }
            return indices32.Where((item, index) => index % DrawConfiguration.VertexLayout.Elements.Length == 0).ToArray();
        }

        public unsafe override Vector3[] GetVertexPositions()
        {
            var positions = new Vector3[GetVertexCount()];
            fixed (Vector3* ptr = &positions[0])
            {
                Marshal.Copy(positionValues, 0, new IntPtr(ptr), positions.Length);
            }
            return positions;
        }

        public override PooledDeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList commandList, DeviceBufferPool? deviceBufferPool = null)
        {
            var indexCount = GetIndexCount();
            var vertexSize = GetVertexSize();
            var vertices = Enumerable.Range(0, indexCount - 1).Select(GetVertexData).SelectMany(x => x).ToArray();
            var desc = new BufferDescription((uint)(indexCount * vertexSize), BufferUsage.VertexBuffer);
            var vb = factory.CreatedPooledBuffer(desc, deviceBufferPool);
            commandList.UpdateBuffer(vb.RealDeviceBuffer, 0, vertices);
            return vb;
        }

        public override PooledDeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList commandList, out int indexCount, DeviceBufferPool? deviceBufferPool = null)
        {
            var indexSize = GetIndexSize();
            indexCount = GetIndexCount();
            var indices = Enumerable.Range(0, indexCount - 1).Select(x => GetIndexData(x).ToArray()).SelectMany(x => x).ToArray();

            var desc = new BufferDescription((uint)indices.Length, BufferUsage.IndexBuffer);
            var ib = factory.CreatedPooledBuffer(desc, deviceBufferPool);
            commandList.UpdateBuffer(ib.RealDeviceBuffer, 0, indices);
            return ib;
        }

        public Span<byte> GetIndexData(int index, VertexElementSemantic semantic = VertexElementSemantic.Position)
        {
            var position = 0;
            var size = GetIndexSize();
            foreach (var element in DrawConfiguration.VertexLayout.Elements)
            {
                if (element.Semantic == semantic)
                    break;

                position += size;
            }
            var count = DrawConfiguration.VertexLayout.Elements.Length;

            return indices.AsSpan(index * size * count + position, size);
        }

        public byte[] GetVertexData(int index)
        {
            var data = new byte[GetVertexSize()];
            var position = 0;
            for(var i = 0; i < DrawConfiguration.VertexLayout.Elements.Length; i++)
            {
                var element = DrawConfiguration.VertexLayout.Elements[i];
                if(element.Semantic == VertexElementSemantic.Position)
                    CopyVertexData(in element, positionValues.AsSpan(), To32BitIndex(GetIndexData(index, VertexElementSemantic.Position)), data.AsSpan(), ref position);
                else if (element.Semantic == VertexElementSemantic.Normal)
                    CopyVertexData(in element, normalValues.AsSpan(), To32BitIndex(GetIndexData(index, VertexElementSemantic.Normal)), data.AsSpan(), ref position);
                else if (element.Semantic == VertexElementSemantic.TextureCoordinate)
                    CopyVertexData(in element, texCoordValues.AsSpan(), To32BitIndex(GetIndexData(index, VertexElementSemantic.TextureCoordinate)), data.AsSpan(), ref position);
                else if (element.Semantic == VertexElementSemantic.Color)
                    CopyVertexData(in element, colorValues.AsSpan(), To32BitIndex(GetIndexData(index, VertexElementSemantic.Color)), data.AsSpan(), ref position);
            }
            return data;
        }

        public override int GetVertexCount()
        {
            if (!DrawConfiguration.VertexLayout.Elements.Any(x => x.Semantic == VertexElementSemantic.Position))
                throw new Exception("A position is required to calculate the vertex count");

            var postionElement = DrawConfiguration.VertexLayout.Elements.First(x => x.Semantic == VertexElementSemantic.Position);
            return positionValues.Length / GetByteCount(postionElement.Format);
        }

        public override int GetIndexCount()
            => indices.Length / GetIndexSize() / DrawConfiguration.VertexLayout.Elements.Length;

        public int GetIndexSize()
            => DrawConfiguration.IndexFormat == IndexFormat.UInt16 ? 2 :
               DrawConfiguration.IndexFormat == IndexFormat.UInt32 ? 4 :
               throw new NotSupportedException();

        public int GetVertexSize()
            => DrawConfiguration.VertexLayout.Elements.Sum(element => GetByteCount(element.Format));

        private static void CopyVertexData(in VertexElementDescription element, Span<byte> sourceData, uint index, Span<byte> targetData, ref int position)
        {
            var size = GetByteCount(element.Format);
            var source = GetData(sourceData, index, size);
            var destination = targetData.Slice(position, size);
            source.CopyTo(destination);
            position += size;
        }

        private static Span<byte> GetData(in Span<byte> data, uint index, int size)
            => data.Slice((int)(index * size), size);

        private static uint To32BitIndex(Span<byte> data)
        {
            if (data.Length == 4)
                return BitConverter.ToUInt32(data);
            if (data.Length == 2)
                return BitConverter.ToUInt16(data);
            throw new NotSupportedException();
        }

        private static byte GetByteCount(VertexElementFormat format)
        {
            return format switch
            {
                VertexElementFormat.Float1 => 4,
                VertexElementFormat.Float2 => 8,
                VertexElementFormat.Float3 => 12,
                VertexElementFormat.Float4 => 16,
                _ => throw new NotSupportedException()
            };
        }

        public static bool operator !=(BinaryMeshData? one, BinaryMeshData? two)
            => !(one == two);

        public static bool operator ==(BinaryMeshData? one, BinaryMeshData? two)
            => EqualsExtensions.EqualsReferenceType(one, two);

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), (positionValues, normalValues, texCoordValues, colorValues, indices).GetHashCode());

        public override bool Equals([NotNullWhen(true)] object? obj)
            => EqualsExtensions.EqualsObject(this, obj);

        public bool Equals(BinaryMeshData? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!base.Equals(other))
                return false;
            if (positionValues == null && other.positionValues != null ||
               positionValues != null && other.positionValues == null ||
               positionValues != null && other.positionValues != null && !other.positionValues.SequenceEqual(positionValues))
                return false;
            if (normalValues == null && other.normalValues != null ||
               normalValues != null && other.normalValues == null ||
               normalValues != null && other.normalValues != null && !other.normalValues.SequenceEqual(normalValues))
                return false;
            if (texCoordValues == null && other.texCoordValues != null ||
               texCoordValues != null && other.texCoordValues == null ||
               texCoordValues != null && other.texCoordValues != null && !other.texCoordValues.SequenceEqual(texCoordValues))
                return false;
            if (colorValues == null && other.colorValues != null ||
               colorValues != null && other.colorValues == null ||
               colorValues != null && other.colorValues != null && !other.colorValues.SequenceEqual(colorValues))
                return false;
            if (indices == null && other.indices != null ||
               indices != null && other.indices == null ||
               indices != null && other.indices != null && !other.indices.SequenceEqual(indices))
                return false;

            return true;
        }

    }
}

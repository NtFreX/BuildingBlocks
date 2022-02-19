using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    // TODO complete use and make ref struct?
    public class BinaryMeshDataProvider : BaseMeshDataProvider
    {
        private readonly byte[] positionValues;
        private readonly byte[] normalValues;
        private readonly byte[] texCoordValues;
        private readonly byte[] colorValues;
        private readonly byte[] indices;

        // TODO: maybe apply transform earlier!!! or move to base class and use in buffer/meshrenderer
        public Matrix4x4 Transform { get; set; }

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

        public static unsafe BinaryMeshDataProvider Create(float[] positionValues, float[] normalValues, float[] texCoordValues, float[] colorValues, uint[] indexValues, VertexLayoutDescription vertexLayout)
        {
            var positions = GetSpan(positionValues);
            var normals = GetSpan(normalValues);
            var texCoords = GetSpan(texCoordValues);
            var colors = GetSpan(colorValues);
            var indices = GetSpan(indexValues);
            return new BinaryMeshDataProvider(positions.ToArray(), normals.ToArray(), texCoords.ToArray(), colors.ToArray(), indices.ToArray())
            {
                IndexFormat = IndexFormat.UInt32,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                VertexLayout = vertexLayout,
                Material = new ()
            };
        }

        public BinaryMeshDataProvider(byte[] positionValues, byte[] normalValues, byte[] texCoordValues, byte[] colorValues, byte[] indices)
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

        public override unsafe bool RayCast(Ray ray, out float distance)
        {
            var indexCount = GetIndexCount();

            distance = float.MaxValue;
            bool result = false;

            fixed (byte* ptr = &positionValues[0])
            {
                for (int i = 0; i < indexCount - 2; i += 3)
                {
                    var v0 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 0));
                    var v1 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 1));
                    var v2 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 2));

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
            var indexCount = GetIndexCount();

            int hits = 0;
            for (int i = 0; i < indexCount - 2; i += 3)
            {
                var v0 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 0));
                var v1 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 1));
                var v2 = GetVertexPositionAt((int)(object)GetIndexPositionAt(i + 2));

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    hits++;
                    distances.Add(newDistance);
                }
            }

            return hits;
        }

        private unsafe uint GetIndexPositionAt(int index)
        {
            fixed (byte* ptr = &indices[0])
            {
                var vertexPtr = ptr + index;

                return *(uint*)ptr;
            }
        }

        private unsafe Vector3 GetVertexPositionAt(int index)
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
                return IndexFormat == IndexFormat.UInt16
                    ? GetIndices16Bit().Select(x => x.Value).ToArray()
                    : GetIndices32Bit().Select(x => { checked { return (ushort)x.Value; } }).ToArray();
            }
        }

        public unsafe override Index16[] GetIndices16Bit()
        {
            if (IndexFormat != IndexFormat.UInt16)
                throw new NotSupportedException();

            var indices16 = new Index16[GetIndexCount() * VertexLayout.Elements.Length];
            fixed (Index16* ptr = &indices16[0])
            {
                Marshal.Copy(indices, 0, new IntPtr(ptr), indices.Length);
            }
            return indices16.Where((item, index) => index % VertexLayout.Elements.Length == 0).ToArray();
        }

        public unsafe override Index32[] GetIndices32Bit()
        {
            if (IndexFormat != IndexFormat.UInt32)
                throw new NotSupportedException();

            var indices32 = new Index32[GetIndexCount() * VertexLayout.Elements.Length];
            fixed (Index32* ptr = &indices32[0])
            {
                Marshal.Copy(indices, 0, new IntPtr(ptr), indices.Length);
            }
            return indices32.Where((item, index) => index % VertexLayout.Elements.Length == 0).ToArray();
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
            foreach (var element in VertexLayout.Elements)
            {
                if (element.Semantic == semantic)
                    break;

                position += size;
            }
            var count = VertexLayout.Elements.Length;

            return indices.AsSpan(index * size * count + position, size);
        }

        public byte[] GetVertexData(int index)
        {
            var data = new byte[GetVertexSize()];
            var position = 0;
            for(var i = 0; i < VertexLayout.Elements.Count(); i++)
            {
                var element = VertexLayout.Elements[i];
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

        public int GetVertexCount()
        {
            if (!VertexLayout.Elements.Any(x => x.Semantic == VertexElementSemantic.Position))
                throw new Exception("A position is required to calculate the vertex count");

            var postionElement = VertexLayout.Elements.First(x => x.Semantic == VertexElementSemantic.Position);
            return positionValues.Length / GetByteCount(postionElement.Format);
        }

        public int GetIndexCount()
            => indices.Length / GetIndexSize() / VertexLayout.Elements.Length;

        public int GetIndexSize()
            => IndexFormat == IndexFormat.UInt16 ? 2 :
               IndexFormat == IndexFormat.UInt32 ? 4 :
               throw new NotSupportedException();

        public int GetVertexSize()
            => VertexLayout.Elements.Sum(element => GetByteCount(element.Format));

        private uint To32BitIndex(Span<byte> data)
        {
            if (data.Length == 4)
                return BitConverter.ToUInt32(data);
            if (data.Length == 2)
                return BitConverter.ToUInt16(data);
            throw new NotSupportedException();
        }

        private void CopyVertexData(in VertexElementDescription element, Span<byte> sourceData, uint index, Span<byte> targetData, ref int position)
        {
            var size = GetByteCount(element.Format);
            var source = GetData(sourceData, index, size);
            var destination = targetData.Slice(position, size);
            source.CopyTo(destination);
            position += size;
        }

        private Span<byte> GetData(in Span<byte> data, uint index, int size)
            => data.Slice((int)(index * size), size);

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

        public static bool operator !=(BinaryMeshDataProvider? one, BinaryMeshDataProvider? two)
            => !(one == two);

        public static bool operator ==(BinaryMeshDataProvider? one, BinaryMeshDataProvider? two)
        {
            if (ReferenceEquals(one, two))
                return true;
            if (ReferenceEquals(one, null))
                return false;
            if (ReferenceEquals(two, null))
                return false;
            return one.Equals(two);
        }

        public bool Equals(BinaryMeshDataProvider? other)
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
            if (!Transform.Equals(other.Transform))
                return false;

            return true;
        }

    }
}

using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    public abstract class MeshDataProvider : MeshData
    {
        public IndexFormat IndexFormat { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; }
        public VertexLayoutDescription VertexLayout { get; set; }
        public string? MaterialName { get; set; }
        public string? TexturePath { get; set; }

        public MaterialInfo Material { get; set; }
        public InstanceInfo[] Instances { get; set; } = InstanceInfo.Single;

        public DeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList cl, out int indexCount) => CreateIndexBuffer(factory, cl, out indexCount, null).RealDeviceBuffer;
        public DeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList cl) => CreateVertexBuffer(factory, cl, null).RealDeviceBuffer;
        public abstract PooledDeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList cl, out int indexCount, DeviceBufferPool? deviceBufferPool = null);
        public abstract PooledDeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList cl, DeviceBufferPool? deviceBufferPool = null);
        public abstract BoundingBox GetBoundingBox();
        public abstract BoundingSphere GetBoundingSphere();
        public abstract ushort[] GetIndices();
        public abstract Index16[] GetIndices16Bit();
        public abstract Index32[] GetIndices32Bit();
        public abstract Vector3[] GetVertexPositions();
        public abstract bool RayCast(Ray ray, out float distance);
        public abstract int RayCast(Ray ray, List<float> distances);
    }

    // TODO complete use and make ref struct?
    public class BinaryMeshDataProvider : MeshDataProvider
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
    }

    public class MeshDataProvider<TVertex, TIndex> : MeshDataProvider, IEquatable<MeshDataProvider<TVertex, TIndex>>
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, IIndex
    {
        private readonly int VertexSize = Marshal.SizeOf(typeof(TVertex));

        public TVertex[] Vertices { get; set; }
        public TIndex[] Indices { get; set; }

        public MeshDataProvider(TVertex[] vertices, TIndex[] indices, PrimitiveTopology primitiveTopology, string? materialName = null, string? texturePath = null, MaterialInfo? material = null)
        {
            Vertices = vertices;
            Indices = indices;
            IndexFormat = TIndex.IndexFormat;
            MaterialName = materialName;
            Material = material ?? new MaterialInfo();
            PrimitiveTopology = primitiveTopology;
            TexturePath = texturePath;
            VertexLayout = TVertex.VertexLayout;
        }

        public virtual async Task SaveAsync(Stream stream)
        {
            stream.WriteByte((byte)PrimitiveTopology);
            stream.WriteByte((byte)(MaterialName == null ? 0 : 1));
            if (MaterialName != null)
            {
                await stream.WriteAsync(BitConverter.GetBytes(MaterialName.Length));
                await stream.WriteAsync(Encoding.UTF8.GetBytes(MaterialName));
            }
            stream.WriteByte((byte)(TexturePath == null ? 0 : 1));
            if (TexturePath != null)
            {
                await stream.WriteAsync(BitConverter.GetBytes(TexturePath.Length));
                await stream.WriteAsync(Encoding.UTF8.GetBytes(TexturePath));
            }
            await stream.WriteAsync(BitConverterExtensions.ToBytes(Material));
            await stream.WriteAsync(BitConverter.GetBytes(Instances.Length));
            foreach(var instance in Instances)
                await stream.WriteAsync(BitConverterExtensions.ToBytes(instance));
            await stream.WriteAsync(BitConverter.GetBytes(Vertices.Length));
            foreach (var vertex in Vertices)
                await stream.WriteAsync(BitConverterExtensions.ToBytes(vertex));
            await stream.WriteAsync(BitConverter.GetBytes(Indices.Length));
            foreach (var index in Indices)
                await stream.WriteAsync(BitConverterExtensions.ToBytes(index));
        }

        public static async Task<MeshDataProvider<TVertex, TIndex>> LoadAsync(Stream stream)
        {
            var primitiveTopology = (PrimitiveTopology)stream.ReadByte();

            string? materialName = null;
            if(stream.ReadByte() == 1)
            {
                var materialBuffer = new byte[Unsafe.SizeOf<char>() * BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() })];
                await stream.ReadAsync(materialBuffer);
                materialName = Encoding.UTF8.GetString(materialBuffer);
            }

            string? texturePath = null;
            if (stream.ReadByte() == 1)
            {
                var textureBuffer = new byte[Unsafe.SizeOf<char>() * BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() })];
                await stream.ReadAsync(textureBuffer);
                texturePath = Encoding.UTF8.GetString(textureBuffer);
            }

            var materialInfoBuffer = new byte[Unsafe.SizeOf<MaterialInfo>()];
            await stream.ReadAsync(materialInfoBuffer);
            var material = BitConverterExtensions.FromBytes<MaterialInfo>(materialInfoBuffer);

            var instanceLength = BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() });
            var instances = new InstanceInfo[instanceLength];
            for (var i = 0; i < instanceLength; i++) 
            {
                var instanceBuffer = new byte[Unsafe.SizeOf<InstanceInfo>()];
                await stream.ReadAsync(instanceBuffer);
                instances[i] = BitConverterExtensions.FromBytes<InstanceInfo>(instanceBuffer);
            }

            var vertexLength = BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() });
            var vertices = new TVertex[vertexLength];
            for (var i = 0; i < vertexLength; i++)
            {
                var vertexBuffer = new byte[Unsafe.SizeOf<TVertex>()];
                await stream.ReadAsync(vertexBuffer);
                vertices[i] = BitConverterExtensions.FromBytes<TVertex>(vertexBuffer);
            }

            var indexLength = BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() });
            var indices = new TIndex[indexLength];
            for (var i = 0; i < indexLength; i++)
            {
                var indexBuffer = new byte[Unsafe.SizeOf<TIndex>()];
                await stream.ReadAsync(indexBuffer);
                indices[i] = BitConverterExtensions.FromBytes<TIndex>(indexBuffer);
            }

            return new MeshDataProvider<TVertex, TIndex>(vertices, indices, primitiveTopology, materialName, texturePath, material) { Instances = instances };
        }

        public override PooledDeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList commandList, DeviceBufferPool? deviceBufferPool = null)
        {
            var desc = new BufferDescription((uint)(Vertices.Length * VertexSize), BufferUsage.VertexBuffer);
            var vb = factory.CreatedPooledBuffer(desc, deviceBufferPool);
            commandList.UpdateBuffer(vb.RealDeviceBuffer, 0, Vertices);
            return vb;
        }

        public override PooledDeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList commandList, out int indexCount, DeviceBufferPool? deviceBufferPool = null)
        {
            var desc = new BufferDescription((uint)(Indices.Length * Marshal.SizeOf(typeof(TIndex))), BufferUsage.IndexBuffer);
            var ib = factory.CreatedPooledBuffer(desc, deviceBufferPool);
            commandList.UpdateBuffer(ib.RealDeviceBuffer, 0, Indices);
            indexCount = Indices.Length;
            return ib;
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
                    ? GetIndices16Bit().Select(x => x.Value).ToArray()
                    : GetIndices32Bit().Select(x => { checked { return (ushort)x.Value; } }).ToArray();
            }
        }

        public override Index16[] GetIndices16Bit()
        {
            if (IndexFormat != IndexFormat.UInt16)
                throw new NotSupportedException();

            return Indices.Cast<Index16>().ToArray();
        }

        public override Index32[] GetIndices32Bit()
        {
            if (IndexFormat != IndexFormat.UInt32)
                throw new NotSupportedException();

            return Indices.Cast<Index32>().ToArray();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var objType = obj.GetType();
            if (objType != typeof(MeshDataProvider<TVertex, TIndex>)) return false;
            return Equals((MeshDataProvider<TVertex, TIndex>)obj);
        }

        public bool Equals(MeshDataProvider<TVertex, TIndex>? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (other.IndexFormat != IndexFormat)
                return false;
            if (other.PrimitiveTopology != PrimitiveTopology)
                return false;
            if (!other.VertexLayout.Equals(VertexLayout))
                return false;
            if (other.MaterialName != MaterialName)
                return false;
            if (other.TexturePath != TexturePath)
                return false;
            if (!other.Material.Equals(Material))
                return false;
            if (Instances == null && other.Instances != null ||
               Instances != null && other.Instances == null ||
               Instances != null && other.Instances != null && !other.Instances.SequenceEqual(Instances))
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

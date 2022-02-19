using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class MeshDataProvider<TVertex, TIndex> : BaseMeshDataProvider, IEquatable<MeshDataProvider<TVertex, TIndex>>
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, IIndex
    {
        private readonly int VertexSize = Marshal.SizeOf(typeof(TVertex));

        public TVertex[] Vertices { get; set; }
        public TIndex[] Indices { get; set; }

        public MeshDataProvider(TVertex[] vertices, TIndex[] indices, PrimitiveTopology primitiveTopology, string? materialName = null, string? texturePath = null, string? alphaMapPath = null, MaterialInfo? material = null)
        {
            Vertices = vertices;
            Indices = indices;
            IndexFormat = TIndex.IndexFormat;
            MaterialName = materialName;
            Material = material ?? new MaterialInfo();
            PrimitiveTopology = primitiveTopology;
            TexturePath = texturePath;
            AlphaMapPath = alphaMapPath;
            VertexLayout = TVertex.VertexLayout;
        }

        public virtual async Task SaveAsync(Stream stream)
        {
            // TODO: support null values
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
            stream.WriteByte((byte)(AlphaMapPath == null ? 0 : 1));
            if (AlphaMapPath != null)
            {
                await stream.WriteAsync(BitConverter.GetBytes(AlphaMapPath.Length));
                await stream.WriteAsync(Encoding.UTF8.GetBytes(AlphaMapPath));
            }
            await stream.WriteAsync(BitConverterExtensions.ToBytes(Material.Value));
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

            string? alphaMapPath = null;
            if (stream.ReadByte() == 1)
            {
                var textureBuffer = new byte[Unsafe.SizeOf<char>() * BitConverter.ToInt32(new byte[] { (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() })];
                await stream.ReadAsync(textureBuffer);
                alphaMapPath = Encoding.UTF8.GetString(textureBuffer);
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

            return new MeshDataProvider<TVertex, TIndex>(vertices, indices, primitiveTopology, materialName, texturePath, alphaMapPath, material) { Instances = instances };
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

        public static bool operator !=(MeshDataProvider<TVertex, TIndex>? one, MeshDataProvider<TVertex, TIndex>? two)
            => !(one == two);

        public static bool operator ==(MeshDataProvider<TVertex, TIndex>? one, MeshDataProvider<TVertex, TIndex>? two)
        {
            if (ReferenceEquals(one, two))
                return true;
            if (ReferenceEquals(one, null))
                return false;
            if (ReferenceEquals(two, null))
                return false;
            return one.Equals(two);
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

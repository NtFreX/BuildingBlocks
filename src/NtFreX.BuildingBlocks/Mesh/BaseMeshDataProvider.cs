using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    public abstract class BaseMeshDataProvider : MeshData
    {
        //TODO: move every but mesh info (material texture, bones, instances to another new class)
        public IndexFormat IndexFormat { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; }
        public VertexLayoutDescription VertexLayout { get; set; }
        public string? MaterialName { get; set; }
        public string? TexturePath { get; set; }
        public string? AlphaMapPath { get; set; }

        public MaterialInfo? Material { get; set; }
        public InstanceInfo[]? Instances { get; set; } = InstanceInfo.Single;
        public BoneInfoVertex[]? Bones { get; set; }
        public Matrix4x4[]? BoneTransforms { get; set; }
        public IBoneAnimationProvider[]? BoneAnimationProviders { get; set; } // Move?

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

        public static bool operator !=(BaseMeshDataProvider? one, BaseMeshDataProvider? two)
            => !(one == two);

        public static bool operator ==(BaseMeshDataProvider? one, BaseMeshDataProvider? two)
        {
            if (ReferenceEquals(one, two))
                return true;
            if (ReferenceEquals(one, null))
                return false;
            if (ReferenceEquals(two, null))
                return false;
            return one.Equals(two);
        }

        public override int GetHashCode() => (IndexFormat, PrimitiveTopology, VertexLayout, MaterialName, TexturePath, AlphaMapPath, Material, Instances, Bones, BoneTransforms, BoneAnimationProviders).GetHashCode();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var objType = obj.GetType();
            if (objType != typeof(BaseMeshDataProvider)) return false;
            return Equals((BaseMeshDataProvider)obj);
        }

        public virtual bool Equals(BaseMeshDataProvider? other)
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
            if (other.AlphaMapPath != AlphaMapPath)
                return false;
            if (!other.Material.Equals(Material))
                return false;
            if ((Instances == null && other.Instances != null) ||
               (Instances != null && other.Instances == null) ||
               (Instances != null && other.Instances != null && !other.Instances.SequenceEqual(Instances)))
                return false;
            if ((Bones == null && other.Bones != null) ||
               (Bones != null && other.Bones == null) ||
               (Bones != null && other.Bones != null && !other.Bones.SequenceEqual(Bones)))
                return false;
            if ((BoneTransforms == null && other.BoneTransforms != null) ||
               (BoneTransforms != null && other.BoneTransforms == null) ||
               (BoneTransforms != null && other.BoneTransforms != null && !other.BoneTransforms.SequenceEqual(BoneTransforms)))
                return false;
            if ((BoneAnimationProviders == null && other.BoneAnimationProviders != null) ||
               (BoneAnimationProviders != null && other.BoneAnimationProviders == null) ||
               (BoneAnimationProviders != null && other.BoneAnimationProviders != null && !other.BoneAnimationProviders.SequenceEqual(BoneAnimationProviders)))
                return false;

            return true;
        }
    }
}

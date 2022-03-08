using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data;

public abstract class SpecializedMeshData : MeshData, IEquatable<SpecializedMeshData>
{
    public bool HasVertexChanges { get; protected set; } = false;
    public bool HasIndexChanges { get; protected set; } = false;
    public DrawConfiguration DrawConfiguration { get; }
    public MeshDataSpecializationDictionary Specializations { get; }

    public SpecializedMeshData(DrawConfiguration drawConfiguration, MeshDataSpecializationDictionary specializations)
    {
        DrawConfiguration = drawConfiguration;
        Specializations = specializations;
    }

    public virtual void UpdateVertexBuffer(CommandList commandList, PooledDeviceBuffer buffer) => HasVertexChanges = false;
    public virtual uint UpdateIndexBuffer(CommandList commandList, PooledDeviceBuffer buffer) { HasIndexChanges = false; return 0; }
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
    public abstract int GetIndexCount();
    public abstract int GetVertexCount();
    public abstract uint GetIndexPositionAt(int index);
    public abstract Vector3 GetVertexPositionAt(uint index);

    public unsafe bool RayCast(Ray ray, out float distance)
    {
        distance = float.MaxValue;
        bool result = false;

        for (int i = 0; i < GetIndexCount() - 2; i += 3)
        {
            var v0 = GetVertexPositionAt(GetIndexPositionAt(i + 0));
            var v1 = GetVertexPositionAt(GetIndexPositionAt(i + 1));
            var v2 = GetVertexPositionAt(GetIndexPositionAt(i + 2));

            if (ray.Intersects(ref v0, ref v1, ref v2, out var newDistance))
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
        for (int i = 0; i < GetIndexCount() - 2; i += 3)
        {
            var v0 = GetVertexPositionAt(GetIndexPositionAt(i + 0));
            var v1 = GetVertexPositionAt(GetIndexPositionAt(i + 1));
            var v2 = GetVertexPositionAt(GetIndexPositionAt(i + 2));

            if (ray.Intersects(ref v0, ref v1, ref v2, out var newDistance))
            {
                hits++;
                distances.Add(newDistance);
            }
        }

        return hits;
    }

    public static bool operator !=(SpecializedMeshData? one, SpecializedMeshData? two)
        => !(one == two);

    public static bool operator ==(SpecializedMeshData? one, SpecializedMeshData? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode() 
        => (DrawConfiguration, Specializations).GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public virtual bool Equals(SpecializedMeshData? other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (other.DrawConfiguration != DrawConfiguration)
            return false;
        if ((Specializations == null && other.Specializations != null) ||
            (Specializations != null && other.Specializations == null) ||
            (Specializations != null && other.Specializations != null && !other.Specializations.SequenceEqual(Specializations)))
            return false;

        return true;
    }
}

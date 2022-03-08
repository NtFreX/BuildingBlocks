using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class InstancedMeshDataSpecialization : MeshDataSpecialization, IEquatable<InstancedMeshDataSpecialization>
{
    private readonly DeviceBufferPool? deviceBufferPool;

    private GraphicsDevice? graphicsDevice;

    public Mutable<InstanceInfo[]> Instances { get; }

    public PooledDeviceBuffer? InstanceBuffer { get; private set; }
    
    public InstancedMeshDataSpecialization(InstanceInfo[] instances, DeviceBufferPool? deviceBufferPool = null)
    {
        this.deviceBufferPool = deviceBufferPool;

        Instances = new Mutable<InstanceInfo[]>(instances, this);
        Instances.ValueChanged += (_, _) => UpdateInstanceBuffer();
    }

    private void UpdateInstanceBuffer()
    {
        if (graphicsDevice == null || InstanceBuffer == null)
            return;

        // TODO: check if updating buffer on command list instead of graphics device is better
        graphicsDevice.UpdateBuffer(InstanceBuffer.RealDeviceBuffer, 0, Instances.Value);
    }

    public static bool operator !=(InstancedMeshDataSpecialization? one, InstancedMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(InstancedMeshDataSpecialization? one, InstancedMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => Instances?.GetHashCode() ?? 0;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(InstancedMeshDataSpecialization? other)
    { 
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if ((Instances == null && other.Instances != null) ||
            (Instances != null && other.Instances == null) ||
            (Instances?.Value != null && other.Instances?.Value != null && !other.Instances.Value.SequenceEqual(Instances.Value)))
            return false;

        return true;
    }

    public override Task CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if (this.graphicsDevice != null)
        {
            Debug.Assert(this.graphicsDevice == graphicsDevice);
            return Task.CompletedTask;
        }

        this.graphicsDevice = graphicsDevice;

        Debug.Assert(Instances.Value != null);

        InstanceBuffer = resourceFactory.GetInstanceBuffer(graphicsDevice, Instances.Value, deviceBufferPool);

        return Task.CompletedTask;
    }

    public override void DestroyDeviceObjects()
    {
        graphicsDevice = null;

        InstanceBuffer?.Destroy();
        InstanceBuffer = null;
    }
}

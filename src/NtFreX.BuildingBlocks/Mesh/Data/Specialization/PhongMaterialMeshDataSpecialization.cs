using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class PhongMaterialMeshDataSpecialization : MeshDataSpecialization, IEquatable<PhongMaterialMeshDataSpecialization>
{
    private readonly DeviceBufferPool? deviceBufferPool;

    private GraphicsDevice? graphicsDevice;

    public Mutable<string?> MaterialName { get; }
    public Mutable<PhongMaterialInfo> Material { get; }

    public ResourceSet? ResouceSet { get; private set; }
    public PooledDeviceBuffer? MaterialBuffer { get; private set; }

    public PhongMaterialMeshDataSpecialization(PhongMaterialInfo material, string? materialName = null, DeviceBufferPool? deviceBufferPool = null)
    {
        this.deviceBufferPool = deviceBufferPool;

        Material = new Mutable<PhongMaterialInfo>(material, this);
        MaterialName = new Mutable<string?>(materialName, this);

        Material.ValueChanged += (_, _) => UpdateMaterialBuffer();
    }

    private void UpdateMaterialBuffer()
    {
        if (graphicsDevice == null || MaterialBuffer == null)
            return;

        graphicsDevice.UpdateBuffer(MaterialBuffer.RealDeviceBuffer, 0, Material.Value);
    }

    public static bool operator !=(PhongMaterialMeshDataSpecialization? one, PhongMaterialMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(PhongMaterialMeshDataSpecialization? one, PhongMaterialMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => (MaterialName, Material).GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(PhongMaterialMeshDataSpecialization? other)
        => other?.MaterialName == MaterialName && other?.Material == Material;

    public override Task CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if (this.graphicsDevice != null)
        {
            Debug.Assert(this.graphicsDevice == graphicsDevice);
            return Task.CompletedTask;
        }

        this.graphicsDevice = graphicsDevice;

        Debug.Assert(Material.Value != null);

        MaterialBuffer = resourceFactory.GetMaterialBuffer(graphicsDevice, Material.Value, "phongmatspecialization", deviceBufferPool);

        var layout = ResourceLayoutFactory.GetMaterialInfoLayout(resourceFactory);
        ResouceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(layout, MaterialBuffer.RealDeviceBuffer), "phongmatresourceset");

        return Task.CompletedTask;
    }

    public override void DestroyDeviceObjects()
    {
        graphicsDevice = null;

        ResouceSet?.Dispose();
        ResouceSet = null;

        MaterialBuffer?.Destroy();
        MaterialBuffer = null;
    }
}

using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class BonesMeshDataSpecialization : MeshDataSpecialization, IEquatable<BonesMeshDataSpecialization>
{
    public const int MaxBoneTransforms = 64;

    private readonly DeviceBufferPool? deviceBufferPool;

    private GraphicsDevice? graphicsDevice;

    public Mutable<BoneInfoVertex[]> Bones { get; }
    public Mutable<Matrix4x4[]> BoneTransforms { get; }
    public IBoneAnimationProvider[] BoneAnimationProviders { get; set; }

    public PooledDeviceBuffer? BonesBuffer { get; private set; }
    public PooledDeviceBuffer? BoneTransformsBuffer { get; private set; }
    public ResourceSet? ResouceSet { get; private set; }

    public BonesMeshDataSpecialization(BoneInfoVertex[] bones, Matrix4x4[] boneTransforms, IBoneAnimationProvider[] boneAnimationProviders, DeviceBufferPool? deviceBufferPool = null)
    {
        this.deviceBufferPool = deviceBufferPool;

        Bones = new Mutable<BoneInfoVertex[]>(bones, this);
        BoneTransforms = new Mutable<Matrix4x4[]>(boneTransforms, this);
        BoneAnimationProviders = boneAnimationProviders;

        Bones.ValueChanged += (_, _) => UpdateBonesBuffer();
        BoneTransforms.ValueChanged += (_, _) => UpdateBoneTransformsBuffer();
    }

    private void UpdateBonesBuffer()
    {
        if (graphicsDevice == null || BonesBuffer == null)
            return;

        graphicsDevice.UpdateBuffer(BonesBuffer.RealDeviceBuffer, 0, Bones.Value);
    }

    private void UpdateBoneTransformsBuffer()
    {
        if (graphicsDevice == null || BoneTransformsBuffer == null)
            return;

        graphicsDevice.UpdateBuffer(BoneTransformsBuffer.RealDeviceBuffer, 0, BoneTransforms.Value);
    }

    public static bool operator !=(BonesMeshDataSpecialization? one, BonesMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(BonesMeshDataSpecialization? one, BonesMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => Bones?.GetHashCode() ?? 0;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(BonesMeshDataSpecialization? other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if ((Bones == null && other.Bones != null) ||
            (Bones != null && other.Bones == null) ||
            (Bones?.Value != null && other.Bones?.Value != null && !other.Bones.Value.SequenceEqual(Bones.Value)))
            return false;
        if ((BoneTransforms == null && other.BoneTransforms != null) ||
            (BoneTransforms != null && other.BoneTransforms == null) ||
            (BoneTransforms?.Value != null && other.BoneTransforms?.Value != null && !other.BoneTransforms.Value.SequenceEqual(BoneTransforms.Value)))
            return false;
        if ((BoneAnimationProviders == null && other.BoneAnimationProviders != null) ||
            (BoneAnimationProviders != null && other.BoneAnimationProviders == null) ||
            (BoneAnimationProviders != null && other.BoneAnimationProviders != null && !other.BoneAnimationProviders.SequenceEqual(BoneAnimationProviders)))
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

        Debug.Assert(Bones.Value != null);
        Debug.Assert(BoneTransforms.Value != null);

        BonesBuffer = resourceFactory.GetBonesInfoBuffer(graphicsDevice, Bones.Value, "bonespecialization", deviceBufferPool);
        BoneTransformsBuffer = resourceFactory.GetBonesTransformBuffer(graphicsDevice, BoneTransforms.Value, "bonespecialization", deviceBufferPool);

        var layout = ResourceLayoutFactory.GetBoneTransformationLayout(resourceFactory);
        ResouceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(layout, BoneTransformsBuffer.RealDeviceBuffer), "bonebonespecialization_resourceset");

        return Task.CompletedTask;
    }

    public override void DestroyDeviceObjects()
    {
        graphicsDevice = null;

        BonesBuffer?.Destroy();
        BonesBuffer = null;

        BoneTransformsBuffer?.Destroy();
        BoneTransformsBuffer = null;

        ResouceSet?.Dispose();
        ResouceSet = null;
    }
}

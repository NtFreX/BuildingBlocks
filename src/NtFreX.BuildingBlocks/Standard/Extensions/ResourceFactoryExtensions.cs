using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class ResourceFactoryExtensions
{
    public static PooledDeviceBuffer CreatedPooledBuffer(this ResourceFactory resourceFactory, BufferDescription bufferDescription, DeviceBufferPool? deviceBufferPool = null)
    {
        return deviceBufferPool == null
            ? new PooledDeviceBuffer(resourceFactory.CreateBuffer(bufferDescription))
            : deviceBufferPool.CreateBuffer(resourceFactory, bufferDescription.Usage, bufferDescription.SizeInBytes);
    }

    public static PooledDeviceBuffer GetInstanceBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, InstanceInfo[] instances, DeviceBufferPool? deviceBufferPool = null)
    {
        var instanceBufferDesc = new BufferDescription((uint)(InstanceInfo.Size * instances.Length), BufferUsage.VertexBuffer);
        var buffer = resourceFactory.CreatedPooledBuffer(instanceBufferDesc, deviceBufferPool);
        graphicsDevice.UpdateBuffer(buffer.RealDeviceBuffer, 0, instances);

        return buffer;
    }

    public static PooledDeviceBuffer GetBonesTransformBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, Matrix4x4[] transforms, DeviceBufferPool? deviceBufferPool = null)
    {
        var bonesBufferDesc = new BufferDescription((uint) Unsafe.SizeOf<Matrix4x4>() * BonesMeshDataSpecialization.MaxBoneTransforms, BufferUsage.UniformBuffer | BufferUsage.Dynamic);
        var buffer = resourceFactory.CreatedPooledBuffer(bonesBufferDesc, deviceBufferPool);
        graphicsDevice.UpdateBuffer(buffer.RealDeviceBuffer, 0, transforms);

        return buffer;
    }

    public static PooledDeviceBuffer GetBonesInfoBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, BoneInfoVertex[] bones, DeviceBufferPool? deviceBufferPool = null)
    {
        var bonesBufferDesc = new BufferDescription((uint)(Unsafe.SizeOf<BoneInfoVertex>() * bones.Length), BufferUsage.VertexBuffer);
        var buffer = resourceFactory.CreatedPooledBuffer(bonesBufferDesc, deviceBufferPool);
        graphicsDevice.UpdateBuffer(buffer.RealDeviceBuffer, 0, bones);

        return buffer;
    }

    private static readonly Dictionary<PhongMaterialInfo, PooledDeviceBuffer> materialBufferCache = new ();
    public static PooledDeviceBuffer GetMaterialBuffer(this ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, PhongMaterialInfo material, DeviceBufferPool? deviceBufferPool = null)
    {
        if(materialBufferCache.TryGetValue(material, out var buffer))
        {
            return buffer;
        }

        var materialBufferDesc = new BufferDescription((uint)Marshal.SizeOf<PhongMaterialInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic);
        var materialBuffer = resourceFactory.CreatedPooledBuffer(materialBufferDesc, deviceBufferPool);
        graphicsDevice.UpdateBuffer(materialBuffer.RealDeviceBuffer, 0, material);
        materialBufferCache.Add(material, materialBuffer);
        return materialBuffer;
    }

}

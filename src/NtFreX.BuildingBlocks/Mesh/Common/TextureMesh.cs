using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

// TODO: delete this type?
public static class TextureMesh
{
    // TODO: if not delete pr
    public static async Task<MeshRenderer> CreateAsync(TextureView texture, Transform? transform = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    {
        var plane = await PlaneMesh.CreateAsync(transform: transform, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
        plane.MeshData.Specializations.AddOrUpdate(new SurfaceTextureMeshDataSpecialization(new StaticTextureProvider(texture)));
        return plane;
    }
}


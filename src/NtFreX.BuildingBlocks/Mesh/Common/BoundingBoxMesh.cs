using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Numerics;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class BoundingBoxMesh
{
    public static Task<MeshRenderer> CreateAsync(BoundingBox boundingBox, float red = 1f, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    {
        var scaleX = boundingBox.Max.X - boundingBox.Min.X;
        var scaleY = boundingBox.Max.Y - boundingBox.Min.Y;
        var scaleZ = boundingBox.Max.Z - boundingBox.Min.Z;
        var posX = boundingBox.Min.X + scaleX / 2f;
        var posY = boundingBox.Min.Y + scaleY / 2f;
        var posZ = boundingBox.Min.Z + scaleZ / 2f;
        return QubeMesh.CreateAsync(red: red, transform: new Transform { Position = new Vector3(posX, posY, posZ), Scale = new Vector3(scaleX, scaleY, scaleZ) }, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool);
    }
}

using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data;

public abstract class MeshDataProvider
{
    public abstract Task<(SpecializedMeshData, BoundingBox)> GetAsync();
}

using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data;

public class StaticMeshDataProvider : MeshDataProvider
{
    private readonly SpecializedMeshData specializedMeshData;

    public StaticMeshDataProvider(SpecializedMeshData specializedMeshData)
    {
        this.specializedMeshData = specializedMeshData;
    }

    public override Task<(SpecializedMeshData, BoundingBox)> GetAsync()
        => Task.FromResult((specializedMeshData, specializedMeshData.GetBoundingBox()));
}

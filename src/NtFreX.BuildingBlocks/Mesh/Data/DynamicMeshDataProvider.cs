using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh.Data;

public class DynamicMeshDataProvider : MeshDataProvider
{
    private readonly Func<SpecializedMeshData> specializedMeshData;

    public DynamicMeshDataProvider(Func<SpecializedMeshData> specializedMeshData)
    {
        this.specializedMeshData = specializedMeshData;
    }

    public override Task<(SpecializedMeshData, BoundingBox)> GetAsync()
    {
        var data = specializedMeshData();
        return Task.FromResult((data, data.GetBoundingBox()));
    }
}

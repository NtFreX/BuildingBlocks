using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public abstract class MeshDataSpecialization 
{
    public virtual Task CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory) => Task.CompletedTask;
    public virtual void DestroyDeviceObjects() { }
}

using Veldrid;

namespace NtFreX.BuildingBlocks.Shell
{
    public interface IShell
    {
        event Func<Task> RenderingAsync;
        event Func<InputSnapshot, Task> UpdatingAsync;
        event Func<GraphicsDevice, ResourceFactory, Swapchain, Task> GraphicsDeviceCreated;
        event Action GraphicsDeviceDestroyed;
        event Action Resized;

        uint Width { get; }
        uint Height { get; }

        bool IsDebug { get; }

        Task RunAsync();
    }
}

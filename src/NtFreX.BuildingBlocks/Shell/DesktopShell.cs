using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NtFreX.BuildingBlocks.Shell
{
    public class DesktopShell : IShell
    {
        private readonly Sdl2Window window;
        private readonly GraphicsDeviceOptions? graphicsDeviceOptions;
        private readonly GraphicsBackend? preferredBackend;

        public event Func<Task>? RenderingAsync;
        public event Action<InputSnapshot>? Updating;
        public event Func<GraphicsDevice, ResourceFactory, Swapchain, Task>? GraphicsDeviceCreated;
        public event Action? GraphicsDeviceDestroyed;
        public event Action? Resized;

        public uint Width => (uint)window.Width;
        public uint Height => (uint)window.Height;

        public bool IsDebug { get; private set; }

        public DesktopShell(WindowCreateInfo windowCreateInfo, GraphicsDeviceOptions? graphicsDeviceOptions = null, GraphicsBackend? preferredBackend = null, bool isDebug = false)
        {
            window = VeldridStartup.CreateWindow(windowCreateInfo);
            window.Resized += () => Resized?.Invoke();
            this.graphicsDeviceOptions = graphicsDeviceOptions;
            this.preferredBackend = preferredBackend;
            IsDebug = isDebug;
        }

        public async Task RunAsync()
        {
           var graphicsDeviceOptions = this.graphicsDeviceOptions ?? new GraphicsDeviceOptions
           {
                SwapchainDepthFormat = PixelFormat.R32_Float,
                HasMainSwapchain = true,
                SwapchainSrgbFormat = true,
                SyncToVerticalBlank = true, //TODO: disable from time to time to make sure perf is top notch
                ResourceBindingModel = ResourceBindingModel.Improved,
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                Debug = IsDebug,
            };

            //TODO: make this work with other graphic backends
            var graphicsDevice = preferredBackend == null ? VeldridStartup.CreateGraphicsDevice(window, graphicsDeviceOptions) : VeldridStartup.CreateGraphicsDevice(window, graphicsDeviceOptions, preferredBackend.Value);
            if (GraphicsDeviceCreated != null)
            {
                await GraphicsDeviceCreated.Invoke(graphicsDevice, graphicsDevice.ResourceFactory, graphicsDevice.MainSwapchain);
            }

            while (window.Exists)
            {
                // TODO: pump window events independetly? (seperate task/thread)
                var inputSnapshot = window.PumpEvents();
                if (window.Exists)
                {
                    Updating?.Invoke(inputSnapshot);
                    //TODO: make this work with await
                    RenderingAsync!.Invoke().Wait();
                    //new Task(async () => await RenderingAsync!.Invoke()).RunSynchronously();
                }
            }

            GraphicsDeviceDestroyed?.Invoke();
        }
    }
}

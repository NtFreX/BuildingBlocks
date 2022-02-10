using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NtFreX.BuildingBlocks.Shell
{
    public class DesktopShell : IShell
    {
        private readonly Sdl2Window window;

        public event Action? Rendering;
        public event Action<InputSnapshot>? Updating;
        public event Func<GraphicsDevice, ResourceFactory, Swapchain, Task>? GraphicsDeviceCreated;
        public event Action? GraphicsDeviceDestroyed;
        public event Action? Resized;

        public uint Width => (uint)window.Width;
        public uint Height => (uint)window.Height;

        public bool IsDebug { get; private set; }

        public DesktopShell(string title, bool isDebug)
        {
            var windowCreateInfo = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = title
            };

            window = VeldridStartup.CreateWindow(windowCreateInfo);
            window.Resized += () => Resized?.Invoke();
            IsDebug = isDebug;
        }

        public async Task RunAsync()
        {
           var graphicsDeviceOptions = new GraphicsDeviceOptions
           {
                SwapchainDepthFormat = PixelFormat.R16_UNorm,
                HasMainSwapchain = true,
                SwapchainSrgbFormat = true,
                SyncToVerticalBlank = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                Debug = IsDebug,
            };

            //TODO: make this work with other graphic backends
            var graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, graphicsDeviceOptions);
            if (GraphicsDeviceCreated != null)
            {
                await GraphicsDeviceCreated.Invoke(graphicsDevice, graphicsDevice.ResourceFactory, graphicsDevice.MainSwapchain);
            }

            while (window.Exists)
            {
                var inputSnapshot = window.PumpEvents();
                if (window.Exists)
                {
                    Updating?.Invoke(inputSnapshot);
                    Rendering?.Invoke();
                }
            }

            GraphicsDeviceDestroyed?.Invoke();
        }
    }
}

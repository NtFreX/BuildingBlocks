using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NtFreX.BuildingBlocks.Shell
{
    public sealed class DesktopShell : IShell, IDisposable
    {
        private readonly SemaphoreSlim startWindowMessagesPumpSemaphore = new (0);
        private readonly SemaphoreSlim endWindowMessagesPumpSemaphore = new (0);
        private readonly SemaphoreSlim windowCreatedSemaphore = new (0);
        private readonly WindowCreateInfo windowCreateInfo;
        private readonly GraphicsDeviceOptions? graphicsDeviceOptions;
        private readonly GraphicsBackend? preferredBackend;

        private Sdl2Window? window;
        private InputSnapshot? currentSnapshot;
        private bool isRunning;

        public event Func<Task>? RenderingAsync;
        public event Func<InputSnapshot, Task>? UpdatingAsync;
        public event Func<GraphicsDevice, ResourceFactory, Swapchain, Task>? GraphicsDeviceCreated;
        public event Action? GraphicsDeviceDestroyed;
        public event Action? Resized;

        public uint Width => (uint)(window?.Width ?? 0);
        public uint Height => (uint)(window?.Height ?? 0);

        public bool IsDebug { get; private set; }

        public DesktopShell(WindowCreateInfo windowCreateInfo, GraphicsDeviceOptions? graphicsDeviceOptions = null, GraphicsBackend? preferredBackend = null, bool isDebug = false)
        {
            this.windowCreateInfo = windowCreateInfo;
            this.graphicsDeviceOptions = graphicsDeviceOptions;
            this.preferredBackend = preferredBackend;

            IsDebug = isDebug;

            var windowThread = new Thread(new ThreadStart(PumpWindowMessages));
            windowThread.Start();
            windowCreatedSemaphore.Wait();
        }

        public async Task RunAsync()
        {
           if (isRunning)
                throw new Exception("The shell was allready started");
           
            isRunning = true;

            var graphicsDeviceOptions = this.graphicsDeviceOptions ?? new GraphicsDeviceOptions
            {
                SwapchainDepthFormat = null,
                HasMainSwapchain = false,
                SwapchainSrgbFormat = true,
                SyncToVerticalBlank = false, 
                ResourceBindingModel = ResourceBindingModel.Improved,
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                Debug = IsDebug,
            };

            //TODO: make this work with other graphic backends
            //TODO: dispose graphics device at some place?
            var graphicsDevice = preferredBackend == null ? VeldridStartup.CreateGraphicsDevice(window, graphicsDeviceOptions) : VeldridStartup.CreateGraphicsDevice(window, graphicsDeviceOptions, preferredBackend.Value);
            if (GraphicsDeviceCreated != null)
            {
                // TODO: run window pump in parallel?
                await GraphicsDeviceCreated.Invoke(graphicsDevice, graphicsDevice.ResourceFactory, graphicsDevice.MainSwapchain);
            }

            Debug.Assert(window != null);
            Debug.Assert(UpdatingAsync != null);
            Debug.Assert(RenderingAsync != null);
            while (window.Exists)
            {
                startWindowMessagesPumpSemaphore.Release();
                await endWindowMessagesPumpSemaphore.WaitAsync();

                if (window.Exists)
                {
                    Debug.Assert(currentSnapshot != null);

                    await UpdatingAsync.Invoke(currentSnapshot);
                    await RenderingAsync.Invoke();
                }
            }

            GraphicsDeviceDestroyed?.Invoke();
        }

        private void PumpWindowMessages()
        {
            window = VeldridStartup.CreateWindow(windowCreateInfo);
            window.Resized += () => Resized?.Invoke();
            windowCreatedSemaphore.Release();

            while (window.Exists)
            {
                startWindowMessagesPumpSemaphore.Wait();
                if (window.Exists)
                {
                    currentSnapshot = window.PumpEvents();
                    endWindowMessagesPumpSemaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            startWindowMessagesPumpSemaphore.Release();
            startWindowMessagesPumpSemaphore.Dispose();
            endWindowMessagesPumpSemaphore.Release();
            endWindowMessagesPumpSemaphore.Dispose();
            windowCreatedSemaphore.Release();
            windowCreatedSemaphore.Dispose();
        }
    }
}

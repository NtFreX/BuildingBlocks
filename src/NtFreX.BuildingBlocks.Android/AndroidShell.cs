using Android.Content;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Shell;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Android
{
    public class AndroidShell<TGame> : IShell
        where TGame : Game, new()
    {
        private readonly ILoggerFactory loggerFactory;

        public VeldridSurfaceView View { get; private set; }

        public uint Width => (uint)View.Width;
        public uint Height => (uint)View.Height;

        public bool IsDebug { get;private set; }

        public event Func<Task>? RenderingAsync;
        public event Func<InputSnapshot, Task>? UpdatingAsync;
        public event Func<GraphicsDevice, ResourceFactory, Swapchain, Task>? GraphicsDeviceCreated;
        public event Action? GraphicsDeviceDestroyed;
        public event Action? Resized;

        public AndroidShell(Context context, ILoggerFactory loggerFactory, bool isDebug)
        {
            this.loggerFactory = loggerFactory;

            IsDebug = isDebug;

            this.View = new VeldridSurfaceView(context, isDebug);
            this.View.RenderingAsync += OnRenderingAsync;
            this.View.DeviceCreated += OnDeviceCreated;
            this.View.Resized += () => Resized?.Invoke();
            this.View.DeviceDisposed += () => GraphicsDeviceDestroyed?.Invoke();
        }

        private void OnDeviceCreated()
        {
            Task.Factory.StartNew(() => RunAsync(), TaskCreationOptions.LongRunning);
        }

        public void OnPause()
        {
            View.OnPause();
        }

        public void OnResume()
        {
            View.OnResume();
        }

        private async Task OnRenderingAsync()
        {
            Debug.Assert(UpdatingAsync != null);
            Debug.Assert(RenderingAsync != null);

            await UpdatingAsync.Invoke(new AndroidInputSnapshot());
            await RenderingAsync.Invoke();
        }

        public async Task RunAsync()
        {
            await Game.SetupShellAsync<TGame>(this, loggerFactory);

            if (GraphicsDeviceCreated != null)
            {
                await GraphicsDeviceCreated.Invoke(View.GraphicsDevice!, View.GraphicsDevice!.ResourceFactory, View.GraphicsDevice.MainSwapchain);
            }

            await View.RunRenderLoopAsync();
        }
    }
}
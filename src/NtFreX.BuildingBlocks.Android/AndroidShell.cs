using Android.Content;
using NtFreX.BuildingBlocks.Shell;
using Veldrid;

namespace NtFreX.BuildingBlocks.Android
{
    public class AndroidShell : IShell
    {
        public VeldridSurfaceView View { get; private set; }

        public uint Width => (uint)View.Width;
        public uint Height => (uint)View.Height;

        public bool IsDebug { get;private set; }

        public event Action? Rendering;
        public event Action<InputSnapshot>? Updating;
        public event Func<GraphicsDevice, ResourceFactory, Swapchain, Task>? GraphicsDeviceCreated;
        public event Action? GraphicsDeviceDestroyed;
        public event Action? Resized;

        public AndroidShell(Context context, bool isDebug)
        {
            this.View = new VeldridSurfaceView(context, isDebug);
            this.View.Rendering += OnRendering;
            this.View.DeviceCreated += OnDeviceCreated;
            this.View.Resized += () => Resized?.Invoke();
            this.View.DeviceDisposed += () => GraphicsDeviceDestroyed?.Invoke();

            IsDebug = isDebug;
        }

        private void OnDeviceCreated()
        {
            _ = RunAsync();
        }

        public void OnPause()
        {
            View.OnPause();
        }

        public void OnResume()
        {
            View.OnResume();
        }

        private void OnRendering()
        {
            Updating?.Invoke(new AndroidInputSnapshot());
            Rendering?.Invoke();
        }

        public async Task RunAsync()
        {
            await Task.Run(async () =>
            {
                if (GraphicsDeviceCreated != null)
                {
                    await GraphicsDeviceCreated.Invoke(View.GraphicsDevice!, View.GraphicsDevice!.ResourceFactory, View.GraphicsDevice.MainSwapchain);
                }
            });
        }
    }
}
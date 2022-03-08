using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Android
{
    public class VeldridSurfaceView : SurfaceView, ISurfaceHolderCallback
    {
        private readonly GraphicsBackend backend;
        protected GraphicsDeviceOptions DeviceOptions { get; }
        private bool surfaceDestroyed;
        private bool paused;
        private bool enabled;
        private bool needsResize;
        private bool surfaceCreated;

        public GraphicsDevice? GraphicsDevice { get; protected set; }
        public Swapchain? MainSwapchain { get; protected set; }

        public event Action? Rendering;
        public event Action? DeviceCreated;
        public event Action? DeviceDisposed;
        public event Action? Resized;

        public VeldridSurfaceView(Context context, bool isDebug) 
            : base(context)
        {
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                isDebug,
                Veldrid.PixelFormat.R16_UNorm,
                false,
                ResourceBindingModel.Improved,
                true,
                true);
            GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.OpenGLES;

            if (!(backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.OpenGLES))
            {
                throw new NotSupportedException($"{backend} is not supported on Android.");
            }

            this.backend = backend;
            DeviceOptions = options;
            Holder?.AddCallback(this);
        }

        public void Disable()
        {
            enabled = false;
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            if (holder.Surface == null)
                throw new ArgumentNullException(nameof(holder.Surface));

            bool deviceCreated = false;
            if (backend == GraphicsBackend.Vulkan)
            {
                if (GraphicsDevice == null)
                {
                    GraphicsDevice = GraphicsDevice.CreateVulkan(DeviceOptions);
                    deviceCreated = true;
                }

                Debug.Assert(MainSwapchain == null);
                SwapchainSource ss = SwapchainSource.CreateAndroidSurface(holder.Surface.Handle, JNIEnv.Handle);
                SwapchainDescription sd = new SwapchainDescription(
                    ss,
                    (uint)Width,
                    (uint)Height,
                    DeviceOptions.SwapchainDepthFormat,
                    DeviceOptions.SyncToVerticalBlank);
                MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(sd);
            }
            else
            {
                Debug.Assert(GraphicsDevice == null && MainSwapchain == null);
                SwapchainSource ss = SwapchainSource.CreateAndroidSurface(holder.Surface.Handle, JNIEnv.Handle);
                SwapchainDescription sd = new SwapchainDescription(
                    ss,
                    (uint)Width,
                    (uint)Height,
                    DeviceOptions.SwapchainDepthFormat,
                    DeviceOptions.SyncToVerticalBlank);
                GraphicsDevice = GraphicsDevice.CreateOpenGLES(DeviceOptions, sd);
                MainSwapchain = GraphicsDevice.MainSwapchain;
                deviceCreated = true;
            }

            if (deviceCreated)
            {
                DeviceCreated?.Invoke();
            }

            surfaceCreated = true;
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            surfaceDestroyed = true;
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            needsResize = true;
        }

        public void RunRenderLoop()
        {
            enabled = true;
            while (enabled)
            {
                try
                {
                    if (paused || !surfaceCreated) { continue; }

                    if (surfaceDestroyed)
                    {
                        HandleSurfaceDestroyed();
                        continue;
                    }

                    if (needsResize)
                    {
                        needsResize = false;
                        MainSwapchain!.Resize((uint)Width, (uint)Height);
                        Resized?.Invoke();
                    }

                    if (GraphicsDevice != null)
                    {
                        Rendering?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }

        private void HandleSurfaceDestroyed()
        {
            if (backend == GraphicsBackend.Vulkan)
            {
                MainSwapchain?.Dispose();
                MainSwapchain = null;
            }
            else
            {
                GraphicsDevice?.Dispose();
                GraphicsDevice = null;
                MainSwapchain = null;
                DeviceDisposed?.Invoke();
            }
        }

        public void OnPause()
        {
            paused = true;
        }

        public void OnResume()
        {
            paused = false;
        }
    }
}
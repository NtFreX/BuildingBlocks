using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model
{
    public abstract class Renderable
    {
        public GraphicsDevice? CurrentGraphicsDevice;
        public ResourceFactory? CurrentResourceFactory;
        public RenderContext? CurrentRenderContext;
        public Scene? CurrentScene;

        private bool shouldRender = true;

        public event EventHandler<bool>? ShouldRenderHasChanged;
        public string? Name { get; set; }

        public virtual RenderPasses RenderPasses => RenderPasses.Forward | RenderPasses.Geometry;

        public bool ShouldRender
        {
            get => shouldRender;
            set
            {
                if (value == shouldRender)
                    return;

                ShouldRenderHasChanged?.Invoke(this, value);
                shouldRender = value;
            }
        }

        //TODO: seperate updateable from per from resource update? currently the pattern is to persist the current graphics device and run updates when they happen, drawbacks benefits?
        //public abstract void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, RenderContext rc);
        public abstract void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass);

        public virtual Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
        {
            if (this.CurrentGraphicsDevice != null)
            {
                Debug.Assert(this.CurrentGraphicsDevice == graphicsDevice);
                return Task.FromResult(false);
            }
            if (this.CurrentResourceFactory != null)
            {
                Debug.Assert(this.CurrentResourceFactory == resourceFactory);
                return Task.FromResult(false);
            }
            if (this.CurrentRenderContext != null)
            {
                Debug.Assert(this.CurrentRenderContext == renderContext);
                return Task.FromResult(false);
            }
            if (this.CurrentScene != null)
            {
                Debug.Assert(this.CurrentScene == scene);
                return Task.FromResult(false);
            }

            CurrentGraphicsDevice = graphicsDevice;
            CurrentResourceFactory = resourceFactory;
            CurrentRenderContext = renderContext;
            CurrentScene = scene;

            return Task.FromResult(true);

        }

        public virtual void DestroyDeviceObjects()
        {
            CurrentGraphicsDevice = null;
            CurrentResourceFactory = null;
            CurrentRenderContext = null;
            CurrentScene = null;
        }

        public abstract RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
    }
}

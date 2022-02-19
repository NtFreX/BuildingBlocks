using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model
{
    public abstract class Renderable : IDisposable
    {
        private bool shouldRender = true;

        public event EventHandler<bool>? ShouldRenderHasChanged;

        public virtual RenderPasses RenderPasses => RenderPasses.Standard;

        public bool ShouldRender
        {
            get => shouldRender;
            set
            {
                ShouldRenderHasChanged?.Invoke(this, value);
                shouldRender = value;
            }
        }

        public abstract void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, RenderContext rc);
        public abstract void Render(GraphicsDevice gd, CommandList cl, RenderContext rc, RenderPasses renderPass);
        public abstract void CreateDeviceObjects(GraphicsDevice gd, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, CommandList cl, RenderContext sc);
        public abstract void DestroyDeviceObjects();
        public abstract RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }
}

using NtFreX.BuildingBlocks.Input;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model.Common
{
    internal class ImGuiRenderer : Renderable, IUpdateable
    {
        private Veldrid.ImGuiRenderer? imguiRenderer;

        private int width;
        private int height;

        public override RenderPasses RenderPasses => RenderPasses.Overlay;

        public ImGuiRenderer(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void WindowResized(int width, int height)  
        {
            this.width = width;
            this.height = height;

            Debug.Assert(imguiRenderer != null);
            imguiRenderer.WindowResized(width, height);
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, CommandList cl, RenderContext rc)
        {
            if (imguiRenderer == null)
            {
                imguiRenderer = new Veldrid.ImGuiRenderer(gd, rc.MainSceneFramebuffer.OutputDescription, width, height, ColorSpaceHandling.Linear);
            }
            else
            {
                imguiRenderer.CreateDeviceResources(gd, rc.MainSceneFramebuffer.OutputDescription, ColorSpaceHandling.Linear);
            }
        }

        public override void DestroyDeviceObjects()
        {
            Debug.Assert(imguiRenderer != null);
            imguiRenderer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
            => new RenderOrderKey(ulong.MaxValue);

        public override void Render(GraphicsDevice gd, CommandList cl, RenderContext rc, RenderPasses renderPass)
        {
            Debug.Assert(imguiRenderer != null);
            Debug.Assert(RenderPasses.HasFlag(renderPass));
            imguiRenderer.Render(gd, cl);
        }

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, RenderContext sc) { }

        public void Update(float deltaSeconds, InputHandler inputHandler)
        {
            Debug.Assert(imguiRenderer != null);
            imguiRenderer.Update(deltaSeconds, inputHandler.CurrentSnapshot);
        }
    }
}

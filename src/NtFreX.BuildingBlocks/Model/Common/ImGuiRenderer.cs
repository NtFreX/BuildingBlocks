using NtFreX.BuildingBlocks.Input;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Model.Common
{
    //TODO: delete and make graphics pipeline node
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

            if (imguiRenderer != null)
            {
                imguiRenderer.WindowResized(width, height);
            }
        }

        public IntPtr GetOrCreateImGuiBinding(ResourceFactory resourceFactory, TextureView textureView)
            => imguiRenderer?.GetOrCreateImGuiBinding(resourceFactory, textureView) ?? throw new Exception("The renderer was not initialized");
        public void RemoveImGuiBinding(TextureView textureView)
            => imguiRenderer?.RemoveImGuiBinding(textureView);

        public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
        {
            if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
                return false;

            Debug.Assert(renderContext.MainSceneFramebuffer != null);

            if (imguiRenderer == null)
            {
                imguiRenderer = new Veldrid.ImGuiRenderer(graphicsDevice, renderContext.MainSceneFramebuffer.OutputDescription, width, height, ColorSpaceHandling.Linear);
            }
            else
            {
                imguiRenderer.CreateDeviceResources(graphicsDevice, renderContext.MainSceneFramebuffer.OutputDescription, ColorSpaceHandling.Linear);
            }

            return true;
        }

        public override void DestroyDeviceObjects()
        {
            base.DestroyDeviceObjects();

            Debug.Assert(imguiRenderer != null);
            imguiRenderer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
            => new (ulong.MaxValue);

        public override void Render(GraphicsDevice gd, CommandList cl, RenderContext rc, RenderPasses renderPass)
        {
            Debug.Assert(imguiRenderer != null);
            Debug.Assert(RenderPasses.HasFlag(renderPass));
            imguiRenderer.Render(gd, cl);
        }

        public void Update(float deltaSeconds, InputHandler inputHandler)
        {
            Debug.Assert(imguiRenderer != null);
            imguiRenderer.Update(deltaSeconds, inputHandler.CurrentSnapshot);
        }
    }
}

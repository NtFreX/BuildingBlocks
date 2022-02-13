using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    public class GraphicsSystem : IDisposable
    {
        private readonly DebugExecutionTimer timerGetVisibleObjects;
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;

        // TODO: support empty camera
        public Mutable<Camera?> Camera { get; }
        public LightSystem LightSystem { get; set; }

        // TODO: support graphics device refresh
        public GraphicsSystem(ILoggerFactory loggerFactory, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Camera camera)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;

            Camera = new Mutable<Camera?>(camera, this);
            LightSystem = new LightSystem(graphicsDevice, resourceFactory);
            timerGetVisibleObjects = new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects"));
        }

        public void Update(float deltaSeconds, InputHandler inputHandler, Scene scene)
        {
            Camera.Value?.BeforeModelUpdate(inputHandler, deltaSeconds);

            foreach(var model in scene.Updateables)
            {
                model.Update(deltaSeconds, inputHandler);
            }

            Camera.Value?.AfterModelUpdate(inputHandler, deltaSeconds);

            LightSystem.Update();
        }

        public async Task DrawAsync(Scene scene)
        {
            if (Camera == null || Camera.Value == null)
                return;

            var renderContext = new RenderContext(graphicsDevice.MainSwapchain.Framebuffer);
            var mainDraw = Task.Run(() =>
            {
                var commandList = resourceFactory.CreateCommandList();
                commandList.Begin();
                commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
                commandList.SetFullViewports();

                float depthClear = graphicsDevice.IsDepthRangeZeroToOne ? 1f : 0f;
                commandList.ClearDepthStencil(depthClear);
                commandList.ClearColorTarget(0, RgbaFloat.Pink);

                var cameraFrustum = new BoundingFrustum(Camera.Value.ViewMatrix * Camera.Value.ProjectionMatrix);
                var mainPassQueue = CreateRenderQueue(scene, cameraFrustum, Camera.Value.Position);

                DrawRenderPass(RenderPasses.Standard, commandList, renderContext, mainPassQueue);
                DrawRenderPass(RenderPasses.AlphaBlend, commandList, renderContext, mainPassQueue);
                DrawRenderPass(RenderPasses.Overlay, commandList, renderContext, mainPassQueue);

                commandList.End();
                graphicsDevice.SubmitCommands(commandList);
                graphicsDevice.SwapBuffers();
                commandList.Dispose();
            });

            await Task.WhenAll(mainDraw);
        }

        public void OnWindowResized(int width, int height)
        {
            if (Camera.Value == null)
                return;

            Camera.Value.WindowWidth.Value = width;
            Camera.Value.WindowHeight.Value = height;
        }

        public void Dispose()
        {
            LightSystem.Dispose();
        }

        private RenderQueue CreateRenderQueue(Scene scene, BoundingFrustum frustum, Vector3 viewPosition)
        {
            var queue = new RenderQueue();
            {
                timerGetVisibleObjects.Start();

                // TODO: make global (mem pressure)
                var frustumItems = new List<Renderable>();
                scene.GetContainedRenderables(frustum, frustumItems);

                queue.AddRange(frustumItems, viewPosition);

                timerGetVisibleObjects.Stop();
            }

            queue.Sort();
            return queue;
        }

        private void DrawRenderPass(RenderPasses renderPass, CommandList commandList, RenderContext renderContext, RenderQueue queue)
        {

            foreach (var model in queue)
            {
                if ((model.RenderPasses & renderPass) != 0)
                {
                    model.Render(graphicsDevice, commandList, renderContext, renderPass);
                }
            }
        }
    }
}

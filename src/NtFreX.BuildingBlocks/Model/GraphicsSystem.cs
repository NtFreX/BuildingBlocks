using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model
{
    public class GraphicsSystem : IDisposable
    {
        private Action mainPassAction;
        private CommandList mainPassCommandList;
        private RenderQueue mainPassQueue = new ();
        private List<Renderable> mainPassFrustumItems = new ();
        private Scene currentScene;
        private RenderContext currentRenderContext = new();

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

            mainPassAction = new Action(ExecuteMainPass);
            mainPassCommandList = resourceFactory.CreateCommandList();
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

            currentScene = scene;
            currentRenderContext.MainSceneFramebuffer  = graphicsDevice.SwapchainFramebuffer;

            await Task.Run(mainPassAction);

            //var mainDraw =
            //await Task.WhenAll(mainDraw);
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
            mainPassCommandList.Dispose();
        }

        private void ExecuteMainPass()
        {
            Debug.Assert(Camera.Value != null);

            mainPassCommandList.Begin();
            mainPassCommandList.SetFramebuffer(currentRenderContext.MainSceneFramebuffer);
            mainPassCommandList.SetFullViewports();

            float depthClear = graphicsDevice.IsDepthRangeZeroToOne ? 0f : 1f;
            mainPassCommandList.ClearDepthStencil(depthClear);
            mainPassCommandList.ClearColorTarget(0, RgbaFloat.Pink);

            var cameraFrustum = new BoundingFrustum(Camera.Value.ViewMatrix * Camera.Value.ProjectionMatrix);
            var mainPassQueue = FillMainPassRenderQueue(currentScene, cameraFrustum, Camera.Value.Position);

            DrawRenderPass(RenderPasses.Standard, mainPassCommandList, currentRenderContext, mainPassQueue);
            DrawRenderPass(RenderPasses.AlphaBlend, mainPassCommandList, currentRenderContext, mainPassQueue);
            DrawRenderPass(RenderPasses.Overlay, mainPassCommandList, currentRenderContext, mainPassQueue);

            mainPassCommandList.End();
            graphicsDevice.SubmitCommands(mainPassCommandList);
            graphicsDevice.SwapBuffers();
        }

        private RenderQueue FillMainPassRenderQueue(Scene scene, BoundingFrustum frustum, Vector3 viewPosition)
        {   
            timerGetVisibleObjects.Start();

            mainPassQueue.Clear();
            mainPassFrustumItems.Clear();

            scene.GetContainedRenderables(frustum, mainPassFrustumItems);

            mainPassQueue.AddRange(mainPassFrustumItems, viewPosition);

            timerGetVisibleObjects.Stop();

            mainPassQueue.Sort();
            return mainPassQueue;
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

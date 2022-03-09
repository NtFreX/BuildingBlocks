using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Material;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

//TODO: rename the name space to graphics?
namespace NtFreX.BuildingBlocks.Model
{
    public sealed class GraphicsSystem
    {
        private readonly SemaphoreSlim shutdownEvent = new(0);
        private readonly SemaphoreSlim mainPassCompletionEvent = new (0);
        private readonly SemaphoreSlim startMainPassSemaphoreSlim = new (0);
        private readonly Task mainPassAction;
        private readonly CommandList[] shadowmapCommandList;
        private readonly CommandList mainCommandList;
        private readonly CommandList mainPassCommandList;
        private readonly RenderQueue mainPassQueue = new ();
        private readonly List<Renderable> mainPassList = new();
        private readonly List<Renderable> mainPassFrustumItems = new ();

        private readonly RenderQueue shadowNearQueue = new();
        private readonly List<Renderable> shadowNearList = new();
        private readonly List<Renderable> shadowNearFrustumItems = new();
        private readonly RenderQueue shadowMidQueue = new();
        private readonly List<Renderable> shadowMidList = new();
        private readonly List<Renderable> shadowMidFrustumItems = new();
        private readonly RenderQueue shadowFarQueue = new();
        private readonly List<Renderable> shadowFarList = new();
        private readonly List<Renderable> shadowFarFrustumItems = new();

        private readonly GraphicsDevice graphicsDevice;
        private readonly Stopwatch stopwatch;
        private uint fbWidth;
        private uint fbHeight;
        private bool isDisposed = false;
        private double lastElapsed = 0;

        public float DrawDeltaModifier { get; set; } = 1f;

        public readonly DebugExecutionTimer TimerGetVisibleObjects;
        public readonly DebugExecutionTimer[] TimerRenderPasses;

        public Scene? DrawingScene { get; private set; }
        public RenderContext? RenderContext { get; private set; }

        public GraphicsSystem(ILoggerFactory loggerFactory, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Stopwatch stopwatch)
        {
            this.graphicsDevice = graphicsDevice;
            this.stopwatch = stopwatch;

            TimerGetVisibleObjects = new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects"), stopwatch);
            TimerRenderPasses = new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem StandardRenderPass"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem AlphaBlendRenderPass"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem OverlayRenderPass"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapNear"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapMid"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapFar"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Mainpass"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Dublicator"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem FullScreen"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Swapchain"), stopwatch)
            };

            //mainPassAction = new Task(new Action(ExecuteMainPass));
            mainPassCommandList = resourceFactory.CreateCommandList();
            mainCommandList = resourceFactory.CreateCommandList();
            shadowmapCommandList = new[] { resourceFactory.CreateCommandList(), resourceFactory.CreateCommandList(), resourceFactory.CreateCommandList() };

            // use a thread here instead of a task because there is no need to reduce ressource usage on the cpu. this enables the decoupling of the draw code from the async state machine which gives another few 0.01% of perf
            mainPassAction = Task.Run(ExecuteMainPassAsync);
        }

        public void Update(float deltaSeconds, InputHandler inputHandler, Scene scene)
        {
            scene.Camera.Value?.BeforeModelUpdate(inputHandler, deltaSeconds);
            
            foreach (var model in scene.Updateables)
            {
                model.Update(deltaSeconds, inputHandler);
            }

            scene.Camera.Value?.AfterModelUpdate(inputHandler, deltaSeconds);
            scene.LightSystem.Value?.Update();
        }

        public async Task DrawAsync(Scene scene, RenderContext renderContext)
        {
            RenderContext = renderContext;
            DrawingScene = scene;

            if (DrawingScene.Camera == null)
                return;

            startMainPassSemaphoreSlim.Release();
            await mainPassCompletionEvent.WaitAsync();
        }

        public void DestroyDeviceResources()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            foreach(var commandList in shadowmapCommandList)
            {
                commandList.Dispose();
            }
            mainPassCommandList.Dispose();
            mainCommandList.Dispose();
            startMainPassSemaphoreSlim.Release();
            mainPassCompletionEvent.Release();
            shutdownEvent.Wait();
            startMainPassSemaphoreSlim.Dispose();
            mainPassCompletionEvent.Dispose();
            mainPassAction.Dispose();
        }

        private void DrawShadowmapPass(
            in float depthClear, in float near, in float far, in Vector3 lightPos, 
            in CommandList commandList, in DeviceBuffer lightViewBuffer, in DeviceBuffer lightProjectionBuffer, in Framebuffer framebuffer,
            in RenderQueue queue, in List<Renderable> furstumItems, in List<Renderable> items, in RenderPasses renderPasses)
        {
            Debug.Assert(DrawingScene != null);
            Debug.Assert(RenderContext != null);

            (var view, var projection) = UpdateDirectionalLightMatrices(graphicsDevice, DrawingScene, near, far, RenderContext.ShadowMapTexture.Width, out BoundingFrustum lightFrustum);

            commandList.Begin();
            commandList.UpdateBuffer(lightViewBuffer, 0, ref view);
            commandList.UpdateBuffer(lightProjectionBuffer, 0, ref projection);
            commandList.SetFramebuffer(framebuffer);
            commandList.SetViewport(0, new Viewport(0, 0, RenderContext.ShadowMapTexture.Width, RenderContext.ShadowMapTexture.Height, 0, 1));
            commandList.SetScissorRect(0, 0, 0, RenderContext.ShadowMapTexture.Width, RenderContext.ShadowMapTexture.Height);
            commandList.ClearDepthStencil(depthClear);

            FillRenderQueue(in queue, in furstumItems, in items, DrawingScene, lightFrustum, lightPos);
            DrawRenderPass(renderPasses, commandList, RenderContext, items);
        }

        private void DrawMainPass(float depthClear)
        {
            // TODO: apply game. DeltaModifier
            var drawDelta = (float)(stopwatch.ElapsedMilliseconds - lastElapsed) * DrawDeltaModifier;

            //TODO: deffered lightninng
            Debug.Assert(RenderContext?.MainSceneFramebuffer != null);
            Debug.Assert(DrawingScene?.Camera.Value != null);

            // TODO: only needs updating when proj changed, move to somewhere else
            var cameraProjection = DrawingScene.Camera.Value.ProjectionMatrix;
            graphicsDevice.UpdateBuffer(RenderContext.CascadeInfoBuffer, 0, new[] {
                Vector4.Transform(new Vector3(0, 0, -CascadedShadowMaps.NearCascadeLimit), cameraProjection).Z,
                Vector4.Transform(new Vector3(0, 0, -CascadedShadowMaps.MidCascadeLimit), cameraProjection).Z,
                Vector4.Transform(new Vector3(0, 0, -Math.Min(DrawingScene.Camera.Value.FarDistance.Value, CascadedShadowMaps.FarCascadeLimit)), cameraProjection).Z,
            });

            //TODO: do at better place?
            graphicsDevice.UpdateBuffer(RenderContext.DrawDeltaBuffer, 0, new[] { drawDelta, 0f, 0f, 0f });

            MaterialTextureFactory.Instance.Run(drawDelta);

            mainPassCommandList.Begin();
            mainPassCommandList.SetFramebuffer(RenderContext.MainSceneFramebuffer);

            var rcWidth = RenderContext.MainSceneFramebuffer.Width;
            var rcHeight = RenderContext.MainSceneFramebuffer.Height;
            mainPassCommandList.SetViewport(0, new Viewport(0, 0, rcWidth, rcHeight, 0, 1));
            mainPassCommandList.SetScissorRect(0, 0, 0, rcWidth, rcHeight);

            mainPassCommandList.ClearDepthStencil(depthClear);
            mainPassCommandList.ClearColorTarget(0, RgbaFloat.Pink);

            TimerGetVisibleObjects.Start();
            var cameraFrustum = new BoundingFrustum(DrawingScene.Camera.Value.ViewMatrix * DrawingScene.Camera.Value.ProjectionMatrix);
            FillRenderQueue(in mainPassQueue, in mainPassFrustumItems, in mainPassList, DrawingScene, cameraFrustum, DrawingScene.Camera.Value.Position);
            TimerGetVisibleObjects.Stop();

            TimerRenderPasses[0].Start();
            DrawRenderPass(RenderPasses.Standard, mainPassCommandList, RenderContext, mainPassList);
            TimerRenderPasses[0].Stop();

            DrawRenderPass(RenderPasses.Particles, mainPassCommandList, RenderContext, mainPassList);

            TimerRenderPasses[1].Start();
            DrawRenderPass(RenderPasses.AlphaBlend, mainPassCommandList, RenderContext, mainPassList);
            TimerRenderPasses[1].Stop();

            TimerRenderPasses[2].Start();
            DrawRenderPass(RenderPasses.Overlay, mainPassCommandList, RenderContext, mainPassList);
            TimerRenderPasses[2].Stop();

            lastElapsed = stopwatch.Elapsed.TotalMilliseconds;
        }

        private async Task ExecuteMainPassAsync()
        {
            while (!isDisposed)
            {
                // no async here. this is our thread! all of it!
                startMainPassSemaphoreSlim.Wait();
                if (isDisposed)
                    break;

                Debug.Assert(DrawingScene?.Camera.Value != null);
                Debug.Assert(RenderContext?.MainSceneFramebuffer != null);
                Debug.Assert(RenderContext?.MainSceneColorTexture != null);
                Debug.Assert(RenderContext?.DuplicatorFramebuffer != null);
                Debug.Assert(DrawingScene.LightSystem.Value != null);

                float depthClear = graphicsDevice.IsDepthRangeZeroToOne ? 0f : 1f;
                /*TODO: directional light position!! */
                Vector3 lightPos = DrawingScene.Camera.Value.Position - DrawingScene.LightSystem.Value.DirectionalLightDirection * 1000f;

                // TODO: make cascade levels dynamic
                // shadows could be done in a single drawing call https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc09/slides/100_Handout%203.pdf
                var shadowMapTaskNear = Task.Run(() =>
                {
                    TimerRenderPasses[3].Start();
                    DrawShadowmapPass(depthClear, DrawingScene.Camera.Value.NearDistance, CascadedShadowMaps.NearCascadeLimit, lightPos,
                        shadowmapCommandList[0], RenderContext.LightViewBufferNear, RenderContext.LightProjectionBufferNear, RenderContext.NearShadowMapFramebuffer,
                        shadowNearQueue, shadowNearFrustumItems, shadowNearList, RenderPasses.ShadowMapNear);
                    TimerRenderPasses[3].Stop();
                });

                var shadowMapTaskMid = Task.Run(() =>
                {
                    TimerRenderPasses[4].Start();
                    DrawShadowmapPass(depthClear, CascadedShadowMaps.NearCascadeLimit, CascadedShadowMaps.MidCascadeLimit, lightPos,
                        shadowmapCommandList[1], RenderContext.LightViewBufferMid, RenderContext.LightProjectionBufferMid, RenderContext.MidShadowMapFramebuffer,
                        shadowMidQueue, shadowMidFrustumItems, shadowMidList, RenderPasses.ShadowMapMid);
                    TimerRenderPasses[4].Stop();
                });

                var shadowMapTaskFar = Task.Run(() =>
                {
                    TimerRenderPasses[5].Start();
                    DrawShadowmapPass(depthClear, CascadedShadowMaps.MidCascadeLimit, Math.Min(DrawingScene.Camera.Value.FarDistance.Value, CascadedShadowMaps.FarCascadeLimit), lightPos,
                        shadowmapCommandList[2], RenderContext.LightViewBufferFar, RenderContext.LightProjectionBufferFar, RenderContext.FarShadowMapFramebuffer,
                        shadowFarQueue, shadowFarFrustumItems, shadowFarList, RenderPasses.ShadowMapFar);
                    TimerRenderPasses[5].Stop();
                });

                var mainTask = Task.Run(() => DrawMainPass(depthClear));

                await Task.WhenAll(shadowMapTaskNear, shadowMapTaskMid, shadowMapTaskFar, mainTask);

                TimerRenderPasses[6].Start();
                shadowmapCommandList[0].End();
                graphicsDevice.SubmitCommands(shadowmapCommandList[0]);
                shadowmapCommandList[1].End();
                graphicsDevice.SubmitCommands(shadowmapCommandList[1]);
                shadowmapCommandList[2].End();
                graphicsDevice.SubmitCommands(shadowmapCommandList[2]);
                mainPassCommandList.End();
                graphicsDevice.SubmitCommands(mainPassCommandList);
                TimerRenderPasses[6].Stop();

                mainPassCompletionEvent.Release();

                TimerRenderPasses[7].Start();
                mainCommandList.Begin();
                if (RenderContext.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
                {
                    mainCommandList.ResolveTexture(RenderContext.MainSceneColorTexture, RenderContext.MainSceneResolvedColorTexture);
                }

                mainCommandList.SetFramebuffer(RenderContext.DuplicatorFramebuffer);
                fbWidth = RenderContext.DuplicatorFramebuffer.Width;
                fbHeight = RenderContext.DuplicatorFramebuffer.Height;
                mainCommandList.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
                mainCommandList.SetViewport(1, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
                mainCommandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
                mainCommandList.SetScissorRect(1, 0, 0, fbWidth, fbHeight);
                DrawRenderPass(RenderPasses.Duplicator, mainCommandList, RenderContext, mainPassList);
                TimerRenderPasses[7].Stop();

                TimerRenderPasses[8].Start();
                mainCommandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
                fbWidth = graphicsDevice.SwapchainFramebuffer.Width;
                fbHeight = graphicsDevice.SwapchainFramebuffer.Height;
                mainCommandList.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
                mainCommandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
                DrawRenderPass(RenderPasses.SwapchainOutput, mainCommandList, RenderContext, mainPassList);
                TimerRenderPasses[8].Start();

                TimerRenderPasses[9].Start();
                mainCommandList.End();
                graphicsDevice.SubmitCommands(mainCommandList);
                graphicsDevice.SwapBuffers();
                TimerRenderPasses[9].Stop();
            }

            shutdownEvent.Release();
        }

        private (Matrix4x4 View, Matrix4x4 Projection) UpdateDirectionalLightMatrices(
            GraphicsDevice gd,
            Scene scene,
            float near,
            float far,
            uint shadowMapWidth,
            out BoundingFrustum lightFrustum)
        {
            Debug.Assert(scene?.Camera.Value != null);
            Debug.Assert(scene?.LightSystem.Value != null);

            //Vector3 lightDir = scene.LightSystem.Value.DirectionalLightDirection;
            //Vector3 viewDir = scene.Camera.Value.LookAt;
            //Vector3 viewPos = scene.Camera.Value.Position;
            //Vector3 up = scene.Camera.Value.Up;
            //FrustumCorners cameraCorners;

            //if (gd.IsDepthRangeZeroToOne)
            //{
            //    FrustumHelpers.ComputePerspectiveFrustumCorners(
            //        ref viewPos,
            //        ref viewDir,
            //        ref up,
            //        scene.Camera.Value.FieldOfView,
            //        far,
            //        near,
            //        scene.Camera.Value.AspectRatio,
            //        out cameraCorners);
            //}
            //else
            //{
            //    FrustumHelpers.ComputePerspectiveFrustumCorners(
            //        ref viewPos,
            //        ref viewDir,
            //        ref up,
            //        scene.Camera.Value.FieldOfView,
            //        near,
            //        far,
            //        scene.Camera.Value.AspectRatio,
            //        out cameraCorners);
            //}

            //// Approach used: http://alextardif.com/
            //Vector3 frustumCenter = Vector3.Zero;
            //frustumCenter += cameraCorners.NearTopLeft;
            //frustumCenter += cameraCorners.NearTopRight;
            //frustumCenter += cameraCorners.NearBottomLeft;
            //frustumCenter += cameraCorners.NearBottomRight;
            //frustumCenter += cameraCorners.FarTopLeft;
            //frustumCenter += cameraCorners.FarTopRight;
            //frustumCenter += cameraCorners.FarBottomLeft;
            //frustumCenter += cameraCorners.FarBottomRight;
            //frustumCenter /= 8f;

            //float radius = (cameraCorners.NearTopLeft - cameraCorners.FarBottomRight).Length() / 2.0f;
            //float texelsPerUnit = shadowMapWidth / (radius * 2.0f);

            //Matrix4x4 scalar = Matrix4x4.CreateScale(texelsPerUnit, texelsPerUnit, texelsPerUnit);

            //Vector3 baseLookAt = -lightDir;

            //Matrix4x4 lookat = Matrix4x4.CreateLookAt(Vector3.Zero, baseLookAt, scene.Camera.Value.Up);
            //lookat = scalar * lookat;
            //Matrix4x4.Invert(lookat, out Matrix4x4 lookatInv);

            //frustumCenter = Vector3.Transform(frustumCenter, lookat);
            //frustumCenter.X = (int)frustumCenter.X;
            //frustumCenter.Y = (int)frustumCenter.Y;
            //frustumCenter = Vector3.Transform(frustumCenter, lookatInv);

            //Vector3 lightPos = frustumCenter - (lightDir * radius * 2f);
            //Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, frustumCenter, scene.Camera.Value.Up);
            Vector3 lightPos = scene.Camera.Value.Position - scene.LightSystem.Value.DirectionalLightDirection * far;
            Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, scene.LightSystem.Value.DirectionalLightDirection + scene.Camera.Value.Position, scene.Camera.Value.Up);

            Matrix4x4 lightProjection = Matrix4x4Extensions.CreateOrthographic(graphicsDevice.IsClipSpaceYInverted, graphicsDevice.IsDepthRangeZeroToOne,
                -far * CascadedShadowMaps.LScale,
                far * CascadedShadowMaps.RScale,
                -far * CascadedShadowMaps.BScale,
                far * CascadedShadowMaps.TScale,
                -far * CascadedShadowMaps.NScale,
                far * CascadedShadowMaps.FScale);

            lightFrustum = new BoundingFrustum(lightView * lightProjection);
            return (lightView, lightProjection);
        }

        private void FillRenderQueue(in RenderQueue queue, in List<Renderable> frustumItems, in List<Renderable> list, Scene scene, BoundingFrustum frustum, Vector3 viewPosition)
        {
            queue.Clear();
            frustumItems.Clear();
            list.Clear();

            scene.GetContainedRenderables(frustum, frustumItems);
            queue.AddRange(frustumItems, viewPosition);
            queue.Sort();

            // TODO: my the hell reverse?? (if not reversing transparency is wrong) also this uses a new list  every time!!!!!!!!!!
            list.AddRange(queue);
            list.Reverse();
        }

        private void DrawRenderPass(RenderPasses renderPass, CommandList commandList, RenderContext renderContext, List<Renderable> queue)
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

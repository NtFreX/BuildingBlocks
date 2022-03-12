using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Material;
using NtFreX.BuildingBlocks.Model.Common;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Utilities;

//TODO: rename the name space to graphics?
namespace NtFreX.BuildingBlocks.Model
{
    public static class GraphicsDeviceExtensions
    {
        public static float GetClearDepth(this GraphicsDevice graphicsDevice) => graphicsDevice.IsDepthRangeZeroToOne ? 0f : 1f;
    }
    public class RenderQueueExtensions
    {
        public static void FillRenderQueue(DebugExecutionTimer frustumCullOrderingExecutionTimer, RenderQueue frutumRenderQueue, List<Renderable> frustumItems, List<Renderable> orderedRenderQueue, Scene scene, BoundingFrustum frustum, Vector3 viewPosition)
        {
            frustumCullOrderingExecutionTimer.Start();
            frustumItems.Clear();
            orderedRenderQueue.Clear();
            frutumRenderQueue.Clear();

            scene.GetContainedRenderables(frustum, frustumItems);
            frutumRenderQueue.AddRange(frustumItems, viewPosition);
            frutumRenderQueue.Sort();

            // TODO: my the hell reverse?? (if not reversing transparency is wrong) also this uses a new list  every time!!!!!!!!!!
            orderedRenderQueue.AddRange(frutumRenderQueue);
            orderedRenderQueue.Reverse();
            frustumCullOrderingExecutionTimer.Stop();
        }

        public static void DrawRenderPass(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass, List<Renderable> queue)
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
    public abstract class GraphicsPipelineNode
    {
        //TODO: this needs to be thread save to function properly

        public string Name { get; }
        public GraphicsPipelineNode[] Children { get; }
        public DebugExecutionTimer[] ExecutionTimers { get; }

        public GraphicsPipelineNode(string name, GraphicsPipelineNode[] children, DebugExecutionTimer[] executionTimers)
        {
            Name = name;
            Children = children;
            ExecutionTimers = executionTimers;
        }

        public abstract Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory);
        public abstract void DestroyDeviceResources();
        public abstract Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext);

    }
    public class ContainerGraphicsPipelineNode : GraphicsPipelineNode
    {
        private readonly List<Task> tasks = new();
        private readonly bool startTask;
        //private readonly CommandListAccessor? submitCommandList;

        public ContainerGraphicsPipelineNode(GraphicsPipelineNode[] children, bool startTask = true/*, CommandListAccessor? submitCommandList = null*/)
            : base("Container", children, Array.Empty<DebugExecutionTimer>())
        {
            this.startTask = startTask;
            //this.submitCommandList = submitCommandList;
        }

        public override async Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            tasks.Clear();
            foreach (var child in Children)
            {
                tasks.Add(child.CreateDeviceResoucesAsync(graphicsDevice, resourceFactory));
            }
            await Task.WhenAll(tasks);
        }

        public override void DestroyDeviceResources()
        {
            foreach (var child in Children)
            {
                child.DestroyDeviceResources();
            }
        }

        public override async Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {

            //if (submitCommandList != null)
            //{
            //    var commandList = submitCommandList.GetOrCreate(resourceFactory);
            //    commandList.Begin();
            //}

            tasks.Clear();
            foreach (var child in Children)
            {
                var childExecutor = new ChildExecutor(delta, graphicsDevice, resourceFactory, scene, renderContext, child);
                Task task = startTask
                    ? Task.Run(childExecutor.ExecuteAsync)
                    : child.ExecuteAsync(delta, graphicsDevice, resourceFactory, scene, renderContext);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            // TODO: generalize
            //if (submitCommandList != null)
            //{
            //    var commandList = submitCommandList.GetOrCreate(resourceFactory);
            //    commandList.End();
            //    graphicsDevice.SubmitCommands(commandList);
            //}
        }

        public class ChildExecutor
        {
            public float Delta;
            public GraphicsDevice GraphicsDevice; 
            public ResourceFactory ResourceFactory; 
            public Scene Scene; 
            public RenderContext RenderContext;
            public GraphicsPipelineNode GraphicsPipelineNode;

            public ChildExecutor(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext, GraphicsPipelineNode graphicsPipelineNode)
            {
                this.Delta = delta;
                this.GraphicsDevice = graphicsDevice;
                this.ResourceFactory = resourceFactory;
                this.Scene = scene;
                this.RenderContext = renderContext;
                this.GraphicsPipelineNode = graphicsPipelineNode;
            }

            public async Task ExecuteAsync() => await GraphicsPipelineNode.ExecuteAsync(Delta, GraphicsDevice, ResourceFactory, Scene, RenderContext);
        }
    }
    public sealed class MaterialGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Material compute pass";

        public MaterialGraphicsPipelineNode(Stopwatch stopwatch, ILoggerFactory loggerFactory)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new [] { new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode " + PassName), stopwatch) }) { }

        public override async Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            await MaterialTextureFactory.Instance.CreateDeviceResourcesAsync(graphicsDevice, resourceFactory);
        }

        public override void DestroyDeviceResources()
        {
            MaterialTextureFactory.Instance.DestroyDeviceResources();
        }

        //TODO: thead safty for multiple graphics pipelines
        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            ExecutionTimers[0].Start();
            MaterialTextureFactory.Instance.Run(delta);
            ExecutionTimers[0].Stop();
            return Task.CompletedTask;
        }
    }
    public sealed class ParticleComputeGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Particle compute pass";

        private CommandList? commandList;
        private Fence? fence;

        private readonly DebugExecutionTimer frustumCullOrderingExecutionTimer;
        private readonly RenderQueue frutumRenderQueue = new();
        private readonly List<Renderable> frustumItems = new();
        private readonly List<Renderable> orderedRenderQueue = new();

        public ParticleComputeGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new[] { new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode " + PassName), stopwatch) }) 
        {
            this.frustumCullOrderingExecutionTimer = frustumCullOrderingExecutionTimer;
        }

        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            commandList = resourceFactory.CreateCommandList();
            commandList.Name = PassName;

            fence = resourceFactory.CreateFence(false);
            fence.Name = PassName;

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            commandList?.Dispose();
            commandList = null;

            fence?.Dispose();
            fence = null;
        }

        //TODO: thead safty for multiple graphics pipelines
        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(commandList != null);
            Debug.Assert(fence != null);
            Debug.Assert(scene.Camera.Value != null);

            ExecutionTimers[0].Start();
            var cameraFrustum = new BoundingFrustum(scene.Camera.Value.ViewMatrix * scene.Camera.Value.ProjectionMatrix);
            RenderQueueExtensions.FillRenderQueue(frustumCullOrderingExecutionTimer, frutumRenderQueue, frustumItems, orderedRenderQueue, scene, cameraFrustum, scene.Camera.Value.Position);

            commandList.Begin();

            foreach(var item in orderedRenderQueue)
            {
                //TODO: get rid of cast
                //TODO: run update buffers in seperate pass
                if (item.RenderPasses == RenderPasses.Particles && item is IParticleRenderer particleRenderer)
                    particleRenderer.Compute(commandList, renderContext);

            }

            commandList.End();
            graphicsDevice.SubmitCommands(commandList, fence);
            graphicsDevice.WaitForFence(fence);
            fence.Reset();
            ExecutionTimers[0].Stop();
            return Task.CompletedTask;
        }
    }
    public sealed class GeometryGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Geometry pass";

        private CommandList? commandList;
        private Fence? fence;

        private readonly DebugExecutionTimer frustumCullOrderingExecutionTimer;
        private readonly RenderQueue frutumRenderQueue = new();
        private readonly List<Renderable> frustumItems = new();
        private readonly List<Renderable> orderedRenderQueue = new();

        public GeometryGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory)
           : base(PassName, Array.Empty<GraphicsPipelineNode>(), new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_setup"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_main"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_alpha"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_submit"), stopwatch),})
        {
            this.frustumCullOrderingExecutionTimer = frustumCullOrderingExecutionTimer;
        }

        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            commandList = resourceFactory.CreateCommandList();
            commandList.Name = PassName;

            fence = resourceFactory.CreateFence(false);
            fence.Name = PassName;

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            commandList?.Dispose();
            commandList = null;

            fence?.Dispose();
            fence = null;
        }

        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            //TODO: complete this work and make it configurable
            Debug.Assert(renderContext.GFramebuffer != null);
            Debug.Assert(commandList != null);
            Debug.Assert(fence != null);
            Debug.Assert(scene.Camera.Value != null);

            ExecutionTimers[0].Start();
            var depthClear = graphicsDevice.GetClearDepth();
            var cameraFrustum = new BoundingFrustum(scene.Camera.Value.ViewMatrix * scene.Camera.Value.ProjectionMatrix);
            RenderQueueExtensions.FillRenderQueue(frustumCullOrderingExecutionTimer, frutumRenderQueue, frustumItems, orderedRenderQueue, scene, cameraFrustum, scene.Camera.Value.Position);
            
            commandList.Begin();
            commandList.SetFramebuffer(renderContext.GFramebuffer);
            commandList.SetViewport(0, new Viewport(0, 0, renderContext.GFramebuffer.Width, renderContext.GFramebuffer.Height, 0, 1));
            commandList.SetScissorRect(0, 0, 0, renderContext.GFramebuffer.Width, renderContext.GFramebuffer.Height);
            commandList.ClearDepthStencil(depthClear);
            commandList.ClearColorTarget(0, RgbaFloat.Pink);
            commandList.ClearColorTarget(1, RgbaFloat.Pink);
            commandList.ClearColorTarget(2, RgbaFloat.Pink);
            ExecutionTimers[0].Stop();

            ExecutionTimers[1].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.Geometry, orderedRenderQueue);
            ExecutionTimers[1].Stop();

            ExecutionTimers[2].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.GeometryAlpha, orderedRenderQueue);
            ExecutionTimers[2].Stop();

            ExecutionTimers[3].Start();
            commandList.End();
            graphicsDevice.SubmitCommands(commandList, fence);
            graphicsDevice.WaitForFence(fence);
            fence.Reset();
            ExecutionTimers[3].Stop();
            return Task.CompletedTask;
        }
    }
    public sealed class SwapchainGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Swapchain pass";

        private readonly bool isDebug;

        private CommandList? commandList;
        private FullScreenQuad? fullScreenQuad;

        public SwapchainGraphicsPipelineNode(Stopwatch stopwatch, ILoggerFactory loggerFactory, bool isDebug)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_setup"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_main"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_submit"), stopwatch),})
        {
            this.isDebug = isDebug;
        }

        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            commandList = resourceFactory.CreateCommandList();
            commandList.Name = PassName;

            commandList.Begin();
            fullScreenQuad = new FullScreenQuad(isDebug);
            fullScreenQuad.CreateDeviceObjects(graphicsDevice, resourceFactory, commandList);
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            commandList?.Dispose();
            commandList = null;
            fullScreenQuad?.DestroyDeviceObjects();
            fullScreenQuad = null;
        }

        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(commandList != null);
            Debug.Assert(fullScreenQuad != null);
            Debug.Assert(renderContext.MainSceneViewResourceSet != null);

            ExecutionTimers[0].Start();
            commandList.Begin();
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            var fbWidth = graphicsDevice.SwapchainFramebuffer.Width;
            var fbHeight = graphicsDevice.SwapchainFramebuffer.Height;
            commandList.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            commandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
            ExecutionTimers[0].Stop();

            ExecutionTimers[1].Start();
            fullScreenQuad.Render(commandList, renderContext.MainSceneViewResourceSet);
            ExecutionTimers[1].Stop();

            ExecutionTimers[2].Start();
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.SwapBuffers();
            ExecutionTimers[2].Stop();

            return Task.CompletedTask;
        }
    }
    public sealed class TextureSamplingGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Texturesampler pass";

        private CommandList? commandList;
        private Fence? fence;

        public TextureSamplingGraphicsPipelineNode(Stopwatch stopwatch, ILoggerFactory loggerFactory)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_main"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_submit"), stopwatch),}) { }

        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            commandList = resourceFactory.CreateCommandList();
            commandList.Name = PassName;
            fence = resourceFactory.CreateFence(false);
            fence.Name = PassName;
            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            commandList?.Dispose();
            commandList = null;
            fence?.Dispose();
            fence = null;
        }

        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(fence != null);
            Debug.Assert(commandList != null);
            Debug.Assert(renderContext.MainSceneColorTexture != null);

            ExecutionTimers[0].Start();
            commandList.Begin();
            if (renderContext.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
            {
                commandList.ResolveTexture(renderContext.MainSceneColorTexture, renderContext.MainSceneResolvedColorTexture);
            }
            ExecutionTimers[0].Stop();

            //mainCommandList.SetFramebuffer(RenderContext.DuplicatorFramebuffer);
            //fbWidth = RenderContext.DuplicatorFramebuffer.Width;
            //fbHeight = RenderContext.DuplicatorFramebuffer.Height;
            //mainCommandList.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            //mainCommandList.SetViewport(1, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            //mainCommandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
            //mainCommandList.SetScissorRect(1, 0, 0, fbWidth, fbHeight);
            //DrawRenderPass(RenderPasses.Duplicator, mainCommandList, RenderContext, mainPassList);

            ExecutionTimers[1].Start();
            commandList.End();
            graphicsDevice.SubmitCommands(commandList, fence);
            graphicsDevice.WaitForFence(fence);
            fence.Reset();
            ExecutionTimers[1].Stop();
            return Task.CompletedTask;
        }
    }
    public sealed class NearShadowMapGraphicsPipelineNode : ShadowMapGraphicsPipelineNode
    {
        public NearShadowMapGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory/*, CommandListAccessor commandListAccessor*/) 
            : base(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory, "near"/*, commandListAccessor*/) { }

        protected override RenderPasses RenderPasses => RenderPasses.ShadowMapNear;
        protected override float Far(Camera camera, RenderContext context) => context.ShadowMaps.NearCascadeLimit; 
        protected override float Near(Camera camera, RenderContext context) => camera.NearDistance;
        protected override Framebuffer Framebuffer(RenderContext renderContext) => renderContext.NearShadowMapFramebuffer;
        protected override DeviceBuffer LightViewBuffer(RenderContext renderContext) => renderContext.LightViewBufferNear;
        protected override DeviceBuffer LightProjectionBuffer(RenderContext renderContext) => renderContext.LightProjectionBufferNear;
    }
    public sealed class MidShadowMapGraphicsPipelineNode : ShadowMapGraphicsPipelineNode
    {
        public MidShadowMapGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory/*, CommandListAccessor commandListAccessor*/)
            : base(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory, "mid"/*, commandListAccessor*/) { }

        protected override RenderPasses RenderPasses => RenderPasses.ShadowMapMid;
        protected override float Far(Camera camera, RenderContext context) => context.ShadowMaps.MidCascadeLimit;
        protected override float Near(Camera camera, RenderContext context) => context.ShadowMaps.NearCascadeLimit;
        protected override Framebuffer Framebuffer(RenderContext renderContext) => renderContext.MidShadowMapFramebuffer;
        protected override DeviceBuffer LightViewBuffer(RenderContext renderContext) => renderContext.LightViewBufferMid;
        protected override DeviceBuffer LightProjectionBuffer(RenderContext renderContext) => renderContext.LightProjectionBufferMid;
    }
    public sealed class FarShadowMapGraphicsPipelineNode : ShadowMapGraphicsPipelineNode
    {
        public FarShadowMapGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory/*, CommandListAccessor commandListAccessor*/)
            : base(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory, "far"/*, commandListAccessor*/) { }

        protected override RenderPasses RenderPasses => RenderPasses.ShadowMapFar;
        protected override float Far(Camera camera, RenderContext context) => Math.Min(camera.FarDistance.Value, context.ShadowMaps.FarCascadeLimit);
        protected override float Near(Camera camera, RenderContext context) => context.ShadowMaps.MidCascadeLimit;
        protected override Framebuffer Framebuffer(RenderContext renderContext) => renderContext.FarShadowMapFramebuffer;
        protected override DeviceBuffer LightViewBuffer(RenderContext renderContext) => renderContext.LightViewBufferFar;
        protected override DeviceBuffer LightProjectionBuffer(RenderContext renderContext) => renderContext.LightProjectionBufferFar;
    }
    //public class CommandListAccessor
    //{
    //    private readonly object locker = new();

    //    private CommandList? commandList;

    //    public void Destroy() 
    //    {
    //        commandList?.Dispose();
    //        commandList = null;
    //    }
    //    public CommandList GetOrCreate(ResourceFactory resourceFactory)
    //    {
    //        lock (locker)
    //        {
    //            if (commandList == null)
    //                commandList = resourceFactory.CreateCommandList();

    //            return commandList;
    //        }
    //    }
    //}

    //TODO: why is this needed, why does paralizing geometry and forward pass work?
    public class SubmitShadowMapGraphicsPipelineNode : GraphicsPipelineNode
    {
        private readonly ShadowMapGraphicsPipelineNode[] shadowMapNodes;

        private Fence[]? fences;
        
        public SubmitShadowMapGraphicsPipelineNode(Stopwatch stopwatch, ILoggerFactory loggerFactory, ShadowMapGraphicsPipelineNode[] shadowMapNodes)
            : base("Submitshadow", Array.Empty<GraphicsPipelineNode>(), new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_Submitshadow_"  + "_submit"), stopwatch),})
        {
            this.shadowMapNodes = shadowMapNodes;
        }


        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            fences = new Fence[shadowMapNodes.Length];
            for (int i = 0; i < fences.Length; i++)
            {
                fences[i] = resourceFactory.CreateFence(false);
                fences[i].Name = "Submitshadowmap";
            }

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            Debug.Assert(fences != null);

            for (int i = 0; i < fences.Length; i++)
            {
                fences[i].Dispose();
            }
            fences = null;
        }

        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(fences != null);

            ExecutionTimers[0].Start();
            var fenceIndex = 0;
            foreach (var node in shadowMapNodes)
            {
                Debug.Assert(node.CommandList != null);

                node.CommandList.End();
                graphicsDevice.SubmitCommands(node.CommandList, fences[fenceIndex++]);
            }
            graphicsDevice.WaitForFences(fences, true);
            foreach(var fence in fences)
            {
                fence.Reset();
            }
            ExecutionTimers[0].Stop();
            return Task.CompletedTask;
        }
    }
    public abstract class ShadowMapGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Shadowmap pass";

        private readonly DebugExecutionTimer frustumCullOrderingExecutionTimer;
        private readonly string subPassName;
        //private readonly CommandListAccessor commandListAccessor;
        private readonly RenderQueue frutumRenderQueue = new();
        private readonly List<Renderable> frustumItems = new();
        private readonly List<Renderable> orderedRenderQueue = new();

        public CommandList? CommandList;

        protected abstract float Near(Camera camera, RenderContext context);
        protected abstract float Far(Camera camera, RenderContext context);
        protected abstract Framebuffer Framebuffer(RenderContext renderContext);
        protected abstract DeviceBuffer LightViewBuffer(RenderContext renderContext);
        protected abstract DeviceBuffer LightProjectionBuffer(RenderContext renderContext);
        protected abstract RenderPasses RenderPasses { get; }

        public ShadowMapGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory, string subPassName/*, CommandListAccessor commandListAccessor*/)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new[] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_setup"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_" + subPassName + "_main"), stopwatch),})
        {
            this.frustumCullOrderingExecutionTimer = frustumCullOrderingExecutionTimer;
            this.subPassName = subPassName;
            //this.commandListAccessor = commandListAccessor;
            //this.subPassName = subPassName;
        }


        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            CommandList = resourceFactory.CreateCommandList();
            CommandList.Name = subPassName + PassName;

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            CommandList?.Dispose();
            CommandList = null;
        }

        //public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory) => Task.CompletedTask;
        //public override void DestroyDeviceResources() => commandListAccessor.Destroy();
        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(scene.Camera.Value != null);
            Debug.Assert(scene.LightSystem.Value != null);
            Debug.Assert(CommandList != null);

            ExecutionTimers[0].Start();
            /*TODO: directional light position!! */
            Vector3 lightPos = scene.Camera.Value.Position - scene.LightSystem.Value.DirectionalLightDirection * 1000f;
            var depthClear = graphicsDevice.GetClearDepth();
            (var view, var projection) = UpdateDirectionalLightMatrices(graphicsDevice, scene, Near(scene.Camera.Value, renderContext), Far(scene.Camera.Value, renderContext), renderContext.ShadowMapTexture.Width, out BoundingFrustum lightFrustum);

            //var commandList = commandListAccessor.GetOrCreate(resourceFactory);

            CommandList.Begin();
            CommandList.UpdateBuffer(LightViewBuffer(renderContext), 0, ref view);
            CommandList.UpdateBuffer(LightProjectionBuffer(renderContext), 0, ref projection);
            CommandList.SetFramebuffer(Framebuffer(renderContext));
            CommandList.SetViewport(0, new Viewport(0, 0, renderContext.ShadowMapTexture.Width, renderContext.ShadowMapTexture.Height, 0, 1));
            CommandList.SetScissorRect(0, 0, 0, renderContext.ShadowMapTexture.Width, renderContext.ShadowMapTexture.Height);
            CommandList.ClearDepthStencil(depthClear);
            ExecutionTimers[0].Stop();

            RenderQueueExtensions.FillRenderQueue(frustumCullOrderingExecutionTimer, frutumRenderQueue, frustumItems, orderedRenderQueue, scene, lightFrustum, lightPos);

            ExecutionTimers[1].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, CommandList, renderContext, RenderPasses, orderedRenderQueue);
            ExecutionTimers[1].Stop();


            return Task.CompletedTask;
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

            Matrix4x4 lightProjection = Matrix4x4Extensions.CreateOrthographic(gd.IsClipSpaceYInverted, gd.IsDepthRangeZeroToOne,
                -far * CascadedShadowMaps.LScale,
                far * CascadedShadowMaps.RScale,
                -far * CascadedShadowMaps.BScale,
                far * CascadedShadowMaps.TScale,
                -far * CascadedShadowMaps.NScale,
                far * CascadedShadowMaps.FScale);

            lightFrustum = new BoundingFrustum(lightView * lightProjection);
            return (lightView, lightProjection);
        }
    }
    public sealed class ForwardRendererGraphicsPipelineNode : GraphicsPipelineNode
    {
        public const string PassName = "Forward main pass";

        private readonly DebugExecutionTimer frustumCullOrderingExecutionTimer;
        private readonly RenderQueue frutumRenderQueue = new();
        private readonly List<Renderable> frustumItems = new();
        private readonly List<Renderable> orderedRenderQueue = new();

        private CommandList? commandList;
        private Fence? fence;

        public ForwardRendererGraphicsPipelineNode(DebugExecutionTimer frustumCullOrderingExecutionTimer, Stopwatch stopwatch, ILoggerFactory loggerFactory)
            : base(PassName, Array.Empty<GraphicsPipelineNode>(), new [] {
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_setup"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_main"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_particles"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_alpha"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_overlay"), stopwatch),
                new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystemNode_" + PassName + "_submit"), stopwatch),})
        {
            this.frustumCullOrderingExecutionTimer = frustumCullOrderingExecutionTimer;
        }

        public override Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            commandList = resourceFactory.CreateCommandList();
            commandList.Name = PassName;

            fence = resourceFactory.CreateFence(false);
            fence.Name = PassName;

            return Task.CompletedTask;
        }
        public override void DestroyDeviceResources()
        {
            commandList?.Dispose();
            commandList = null;

            fence?.Dispose();
            fence = null;
        }

        public override Task ExecuteAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(renderContext.MainSceneFramebuffer != null);
            Debug.Assert(commandList != null);
            Debug.Assert(scene.Camera.Value != null);

            ExecutionTimers[0].Start();
            commandList.Begin();
            commandList.SetFramebuffer(renderContext.MainSceneFramebuffer);

            var rcWidth = renderContext.MainSceneFramebuffer.Width;
            var rcHeight = renderContext.MainSceneFramebuffer.Height;
            commandList.SetViewport(0, new Viewport(0, 0, rcWidth, rcHeight, 0, 1));
            commandList.SetScissorRect(0, 0, 0, rcWidth, rcHeight);

            var depthClear = graphicsDevice.GetClearDepth();
            commandList.ClearDepthStencil(depthClear);
            commandList.ClearColorTarget(0, RgbaFloat.Pink);
            ExecutionTimers[0].Stop();

            var cameraFrustum = new BoundingFrustum(scene.Camera.Value.ViewMatrix * scene.Camera.Value.ProjectionMatrix);
            RenderQueueExtensions.FillRenderQueue(frustumCullOrderingExecutionTimer, frutumRenderQueue, frustumItems, orderedRenderQueue, scene, cameraFrustum, scene.Camera.Value.Position);

            ExecutionTimers[1].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.Forward, orderedRenderQueue);
            ExecutionTimers[1].Stop();

            ExecutionTimers[2].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.Particles, orderedRenderQueue);
            ExecutionTimers[2].Stop();

            ExecutionTimers[3].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.AlphaBlend, orderedRenderQueue);
            ExecutionTimers[3].Stop();

            ExecutionTimers[4].Start();
            RenderQueueExtensions.DrawRenderPass(graphicsDevice, commandList, renderContext, RenderPasses.Overlay, orderedRenderQueue);
            ExecutionTimers[4].Stop();

            ExecutionTimers[5].Start();
            commandList.End();
            graphicsDevice.SubmitCommands(commandList, fence);
            graphicsDevice.WaitForFence(fence);
            fence.Reset();
            ExecutionTimers[5].Stop();
            return Task.CompletedTask;
        }
    }
    public sealed class GraphicsSystem
    {
        public GraphicsPipelineNode[] Nodes { get; private set; }

        private CommandList? prepareCommandList;

        private DebugExecutionTimer frustumCullOrderingExecutionTimer;

        public GraphicsSystem(ILoggerFactory loggerFactory, Stopwatch stopwatch, bool isDebug)
        {
            frustumCullOrderingExecutionTimer = new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects"), stopwatch);
            //var shadowCommandList = new CommandListAccessor();

            var shadowMapNodes = new ShadowMapGraphicsPipelineNode[]
            {
                new NearShadowMapGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory/*, shadowCommandList*/),
                new MidShadowMapGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory/*, shadowCommandList*/),
                new FarShadowMapGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory/*, shadowCommandList*/),
            };

            var precompute = new List<GraphicsPipelineNode>(shadowMapNodes);
            precompute.Add(new MaterialGraphicsPipelineNode(stopwatch, loggerFactory));
            precompute.Add(new ParticleComputeGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory));

            Nodes = new GraphicsPipelineNode[]
            {
                new ContainerGraphicsPipelineNode(precompute.ToArray()/*, submitCommandList: shadowCommandList*/),
                new SubmitShadowMapGraphicsPipelineNode(stopwatch, loggerFactory, shadowMapNodes),

                new ContainerGraphicsPipelineNode(new GraphicsPipelineNode[]
                {
                    new ForwardRendererGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory),
                    new GeometryGraphicsPipelineNode(frustumCullOrderingExecutionTimer, stopwatch, loggerFactory)
                }),
                new TextureSamplingGraphicsPipelineNode(stopwatch, loggerFactory),
                new SwapchainGraphicsPipelineNode(stopwatch, loggerFactory, isDebug)
            };
        }

        public string GetCurrentExecutionTimes(string prefix, string timeFormat)
        {
            var text = prefix + frustumCullOrderingExecutionTimer.Source.AverageMilliseconds.ToString(timeFormat) + "ms " + frustumCullOrderingExecutionTimer.Source.Name + Environment.NewLine + 
                       GetCurrentExecutionTimes(prefix, timeFormat, Nodes);

            return text;
        }
        private string GetCurrentExecutionTimes(string prefix, string timeFormat, GraphicsPipelineNode[] nodes)
        {
            var text = new StringBuilder();
            foreach(var node in nodes)
            {
                foreach (var timer in node.ExecutionTimers)
                {
                    text.AppendLine(prefix + timer.Source.AverageMilliseconds.ToString(timeFormat) + "ms " + timer.Source.Name);
                }
                var subText = GetCurrentExecutionTimes(prefix, timeFormat, node.Children);
                if(!string.IsNullOrEmpty(subText))
                    text.AppendLine(subText);
            }
            return text.ToString();
        }

        public async Task CreateDeviceResoucesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            foreach (var node in Nodes)
            {
                await node.CreateDeviceResoucesAsync(graphicsDevice, resourceFactory);
            }
            prepareCommandList = resourceFactory.CreateCommandList();
        }

        public void DestroyDeviceResouces()
        {
            prepareCommandList?.Dispose();
            prepareCommandList = null;
            foreach (var node in Nodes)
            {
                node.DestroyDeviceResources();
            }
        }

        public async Task DrawAsync(float delta, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Scene scene, RenderContext renderContext)
        {
            Debug.Assert(prepareCommandList != null);
            Debug.Assert(scene.Camera.Value != null);

            //TODO: do not pass delta * 1000 pass delta direcctly and adapt current particle systems and materials
            var drawDelta = delta * 1000;

            prepareCommandList.Begin();
            // TODO: only needs updating when proj changed, move to somewhere else
            prepareCommandList.UpdateBuffer(renderContext.CascadeInfoBuffer, 0, new[] {
                    Vector4.Transform(new Vector3(0, 0, -renderContext.ShadowMaps.NearCascadeLimit), scene.Camera.Value.ProjectionMatrix).Z,
                    Vector4.Transform(new Vector3(0, 0, -renderContext.ShadowMaps.MidCascadeLimit), scene.Camera.Value.ProjectionMatrix).Z,
                    Vector4.Transform(new Vector3(0, 0, -Math.Min(scene.Camera.Value.FarDistance.Value, renderContext.ShadowMaps.FarCascadeLimit)), scene.Camera.Value.ProjectionMatrix).Z,
                });

            //TODO: do at better place?
            prepareCommandList.UpdateBuffer(renderContext.DrawDeltaBuffer, 0, new[] { drawDelta, 0f, 0f, 0f });
            prepareCommandList.End();
            graphicsDevice.SubmitCommands(prepareCommandList);

            foreach (var node in Nodes)
            {
                await node.ExecuteAsync(drawDelta, graphicsDevice, resourceFactory, scene, renderContext);
            }
        }
    }

    //public sealed class GraphicsSystem2
    //{
    //    private readonly SemaphoreSlim shutdownEvent = new(0);
    //    private readonly SemaphoreSlim mainPassCompletionEvent = new (0);
    //    private readonly SemaphoreSlim startMainPassSemaphoreSlim = new (0);
    //    private readonly Task mainPassAction;
    //    private readonly CommandList prepareCommandList;
    //    private readonly CommandList[] shadowmapCommandList;
    //    private readonly CommandList swapchainCommandList;
    //    private readonly CommandList mainCommandList;
    //    private readonly CommandList gBufferCommandList;
    //    private readonly CommandList mainPassCommandList;
    //    private readonly RenderQueue mainPassQueue = new ();
    //    private readonly List<Renderable> mainPassList = new();
    //    private readonly List<Renderable> mainPassFrustumItems = new ();

    //    private readonly RenderQueue shadowNearQueue = new();
    //    private readonly List<Renderable> shadowNearList = new();
    //    private readonly List<Renderable> shadowNearFrustumItems = new();
    //    private readonly RenderQueue shadowMidQueue = new();
    //    private readonly List<Renderable> shadowMidList = new();
    //    private readonly List<Renderable> shadowMidFrustumItems = new();
    //    private readonly RenderQueue shadowFarQueue = new();
    //    private readonly List<Renderable> shadowFarList = new();
    //    private readonly List<Renderable> shadowFarFrustumItems = new();

    //    private readonly GraphicsDevice graphicsDevice;
    //    private readonly Stopwatch stopwatch;
    //    private uint fbWidth;
    //    private uint fbHeight;
    //    private bool isDisposed = false;
    //    private double lastMainPassElapsed = 0;

    //    public float DrawDeltaModifier { get; set; } = 1f;
    //    public Viewport? Viewport { get; set; }

    //    public readonly DebugExecutionTimer TimerGetVisibleObjects;
    //    public readonly DebugExecutionTimer[] TimerRenderPasses;

    //    public Scene? DrawingScene { get; private set; }
    //    public RenderContext? RenderContext { get; private set; }

    //    public GraphicsSystem(ILoggerFactory loggerFactory, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Stopwatch stopwatch)
    //    {
    //        this.graphicsDevice = graphicsDevice;
    //        this.stopwatch = stopwatch;

    //        TimerGetVisibleObjects = new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects"), stopwatch);
    //        TimerRenderPasses = new[] {
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem StandardRenderPass"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem AlphaBlendRenderPass"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem OverlayRenderPass"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapNear"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapMid"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem ShadowmapFar"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Mainpass"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Dublicator"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem FullScreen"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Swapchain"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Material"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Particle"), stopwatch),
    //            new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem Prepare"), stopwatch)
    //        };

    //        //mainPassAction = new Task(new Action(ExecuteMainPass));
    //        gBufferCommandList = resourceFactory.CreateCommandList();
    //        mainPassCommandList = resourceFactory.CreateCommandList();
    //        mainCommandList = resourceFactory.CreateCommandList();
    //        prepareCommandList = resourceFactory.CreateCommandList();
    //        swapchainCommandList = resourceFactory.CreateCommandList();
    //        shadowmapCommandList = new[] { resourceFactory.CreateCommandList(), resourceFactory.CreateCommandList(), resourceFactory.CreateCommandList() };

    //        // use a thread here instead of a task because there is no need to reduce ressource usage on the cpu. this enables the decoupling of the draw code from the async state machine which gives another few 0.01% of perf
    //        mainPassAction = Task.Run(ExecuteMainPassAsync);
    //    }

    //    //TODO: move to scene

    //    public async Task DrawAsync(Scene scene, RenderContext renderContext)
    //    {
    //        RenderContext = renderContext;
    //        DrawingScene = scene;

    //        if (DrawingScene.Camera == null)
    //            return;

    //        startMainPassSemaphoreSlim.Release();
    //        await mainPassCompletionEvent.WaitAsync();
    //    }

    //    public void DestroyDeviceResources()
    //    {
    //        if (isDisposed)
    //            return;

    //        isDisposed = true;

    //        foreach(var commandList in shadowmapCommandList)
    //        {
    //            commandList.Dispose();
    //        }
    //        swapchainCommandList.Dispose();
    //        prepareCommandList.Dispose();
    //        mainPassCommandList.Dispose();
    //        gBufferCommandList.Dispose();
    //        mainCommandList.Dispose();
    //        startMainPassSemaphoreSlim.Release();
    //        mainPassCompletionEvent.Release();
    //        shutdownEvent.Wait();
    //        startMainPassSemaphoreSlim.Dispose();
    //        mainPassCompletionEvent.Dispose();
    //        mainPassAction.Dispose();
    //    }

    //    private void DrawShadowmapPass(
    //        in float depthClear, in float near, in float far, in Vector3 lightPos, 
    //        in CommandList commandList, in DeviceBuffer lightViewBuffer, in DeviceBuffer lightProjectionBuffer, in Framebuffer framebuffer,
    //        in RenderQueue queue, in List<Renderable> furstumItems, in List<Renderable> items, in RenderPasses renderPasses)
    //    {
    //        Debug.Assert(DrawingScene != null);
    //        Debug.Assert(RenderContext != null);

    //        (var view, var projection) = UpdateDirectionalLightMatrices(graphicsDevice, DrawingScene, near, far, RenderContext.ShadowMapTexture.Width, out BoundingFrustum lightFrustum);

    //        commandList.Begin();
    //        commandList.UpdateBuffer(lightViewBuffer, 0, ref view);
    //        commandList.UpdateBuffer(lightProjectionBuffer, 0, ref projection);
    //        commandList.SetFramebuffer(framebuffer);
    //        commandList.SetViewport(0, new Viewport(0, 0, RenderContext.ShadowMapTexture.Width, RenderContext.ShadowMapTexture.Height, 0, 1));
    //        commandList.SetScissorRect(0, 0, 0, RenderContext.ShadowMapTexture.Width, RenderContext.ShadowMapTexture.Height);
    //        commandList.ClearDepthStencil(depthClear);

    //        FillRenderQueue(in queue, in furstumItems, in items, DrawingScene, lightFrustum, lightPos);
    //        DrawRenderPass(renderPasses, commandList, RenderContext, items);
    //    }

    //    private void DrawMainPass(float depthClear, float drawDelta)
    //    {
    //        //TODO: deffered lightninng
    //        Debug.Assert(RenderContext?.MainSceneFramebuffer != null);
    //        Debug.Assert(DrawingScene?.Camera.Value != null);

    //        TimerRenderPasses[10].Start();
    //        MaterialTextureFactory.Instance.Run(drawDelta);
    //        TimerRenderPasses[10].Stop();

    //        mainPassCommandList.Begin();
    //        mainPassCommandList.SetFramebuffer(RenderContext.MainSceneFramebuffer);

    //        var rcWidth = RenderContext.MainSceneFramebuffer.Width;
    //        var rcHeight = RenderContext.MainSceneFramebuffer.Height;
    //        mainPassCommandList.SetViewport(0, new Viewport(0, 0, rcWidth, rcHeight, 0, 1));
    //        mainPassCommandList.SetScissorRect(0, 0, 0, rcWidth, rcHeight);

    //        mainPassCommandList.ClearDepthStencil(depthClear);
    //        mainPassCommandList.ClearColorTarget(0, RgbaFloat.Pink);

    //        var cameraFrustum = new BoundingFrustum(DrawingScene.Camera.Value.ViewMatrix * DrawingScene.Camera.Value.ProjectionMatrix);
    //        FillRenderQueue(in mainPassQueue, in mainPassFrustumItems, in mainPassList, DrawingScene, cameraFrustum, DrawingScene.Camera.Value.Position);

    //        TimerRenderPasses[0].Start();
    //        DrawRenderPass(RenderPasses.Forward, mainPassCommandList, RenderContext, mainPassList);
    //        TimerRenderPasses[0].Stop();

    //        TimerRenderPasses[11].Start();
    //        DrawRenderPass(RenderPasses.Particles, mainPassCommandList, RenderContext, mainPassList);
    //        TimerRenderPasses[11].Stop();

    //        TimerRenderPasses[1].Start();
    //        DrawRenderPass(RenderPasses.AlphaBlend, mainPassCommandList, RenderContext, mainPassList);
    //        TimerRenderPasses[1].Stop();

    //        TimerRenderPasses[2].Start();
    //        DrawRenderPass(RenderPasses.Overlay, mainPassCommandList, RenderContext, mainPassList);
    //        TimerRenderPasses[2].Stop();


    //        //TODO: complete this work and make it configurable
    //        Debug.Assert(RenderContext.GFramebuffer != null);
    //        gBufferCommandList.Begin();
    //        gBufferCommandList.SetFramebuffer(RenderContext.GFramebuffer);
    //        gBufferCommandList.SetViewport(0, new Viewport(0, 0, RenderContext.GFramebuffer.Width, RenderContext.GFramebuffer.Height, 0, 1));
    //        gBufferCommandList.SetScissorRect(0, 0, 0, RenderContext.GFramebuffer.Width, RenderContext.GFramebuffer.Height);
    //        gBufferCommandList.ClearDepthStencil(depthClear);
    //        gBufferCommandList.ClearColorTarget(0, RgbaFloat.Pink);
    //        gBufferCommandList.ClearColorTarget(1, RgbaFloat.Pink);
    //        gBufferCommandList.ClearColorTarget(2, RgbaFloat.Pink);
    //        DrawRenderPass(RenderPasses.Geometry, gBufferCommandList, RenderContext, mainPassList);
    //        DrawRenderPass(RenderPasses.GeometryAlpha, gBufferCommandList, RenderContext, mainPassList);
    //        gBufferCommandList.End();
    //        graphicsDevice.SubmitCommands(gBufferCommandList);



    //        lastMainPassElapsed = stopwatch.Elapsed.TotalMilliseconds;
    //    }

    //    private async Task ExecuteMainPassAsync()
    //    {
    //        while (!isDisposed)
    //        {
    //            // no async here. this is our thread! all of it!
    //            startMainPassSemaphoreSlim.Wait();
    //            if (isDisposed)
    //                break;

    //            TimerRenderPasses[12].Start();
    //            Debug.Assert(DrawingScene?.Camera.Value != null);
    //            Debug.Assert(RenderContext?.MainSceneFramebuffer != null);
    //            Debug.Assert(RenderContext?.MainSceneColorTexture != null);
    //            Debug.Assert(RenderContext?.DuplicatorFramebuffer != null);
    //            Debug.Assert(DrawingScene.LightSystem.Value != null);

    //            /*TODO: directional light position!! */
    //            Vector3 lightPos = DrawingScene.Camera.Value.Position - DrawingScene.LightSystem.Value.DirectionalLightDirection * 1000f;
    //            float depthClear = graphicsDevice.IsDepthRangeZeroToOne ? 0f : 1f;
    //            var drawDelta = (float)(stopwatch.ElapsedMilliseconds - lastMainPassElapsed) * DrawDeltaModifier;
    //            var cameraProjection = DrawingScene.Camera.Value.ProjectionMatrix;

    //            prepareCommandList.Begin();
    //            // TODO: only needs updating when proj changed, move to somewhere else
    //            prepareCommandList.UpdateBuffer(RenderContext.CascadeInfoBuffer, 0, new[] {
    //                Vector4.Transform(new Vector3(0, 0, -CascadedShadowMaps.NearCascadeLimit), cameraProjection).Z,
    //                Vector4.Transform(new Vector3(0, 0, -CascadedShadowMaps.MidCascadeLimit), cameraProjection).Z,
    //                Vector4.Transform(new Vector3(0, 0, -Math.Min(DrawingScene.Camera.Value.FarDistance.Value, CascadedShadowMaps.FarCascadeLimit)), cameraProjection).Z,
    //            });

    //            //TODO: do at better place?
    //            prepareCommandList.UpdateBuffer(RenderContext.DrawDeltaBuffer, 0, new[] { drawDelta, 0f, 0f, 0f });
    //            prepareCommandList.End();
    //            graphicsDevice.SubmitCommands(prepareCommandList);

    //            // TODO: make cascade levels dynamic
    //            // shadows could be done in a single drawing call https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc09/slides/100_Handout%203.pdf
    //            var shadowMapTaskNear = Task.Run(() =>
    //            {
    //                TimerRenderPasses[3].Start();
    //                DrawShadowmapPass(depthClear, DrawingScene.Camera.Value.NearDistance, CascadedShadowMaps.NearCascadeLimit, lightPos,
    //                    shadowmapCommandList[0], RenderContext.LightViewBufferNear, RenderContext.LightProjectionBufferNear, RenderContext.NearShadowMapFramebuffer,
    //                    shadowNearQueue, shadowNearFrustumItems, shadowNearList, RenderPasses.ShadowMapNear);
    //                TimerRenderPasses[3].Stop();
    //            });

    //            var shadowMapTaskMid = Task.Run(() =>
    //            {
    //                TimerRenderPasses[4].Start();
    //                DrawShadowmapPass(depthClear, CascadedShadowMaps.NearCascadeLimit, CascadedShadowMaps.MidCascadeLimit, lightPos,
    //                    shadowmapCommandList[1], RenderContext.LightViewBufferMid, RenderContext.LightProjectionBufferMid, RenderContext.MidShadowMapFramebuffer,
    //                    shadowMidQueue, shadowMidFrustumItems, shadowMidList, RenderPasses.ShadowMapMid);
    //                TimerRenderPasses[4].Stop();
    //            });

    //            var shadowMapTaskFar = Task.Run(() =>
    //            {
    //                TimerRenderPasses[5].Start();
    //                DrawShadowmapPass(depthClear, CascadedShadowMaps.MidCascadeLimit, Math.Min(DrawingScene.Camera.Value.FarDistance.Value, CascadedShadowMaps.FarCascadeLimit), lightPos,
    //                    shadowmapCommandList[2], RenderContext.LightViewBufferFar, RenderContext.LightProjectionBufferFar, RenderContext.FarShadowMapFramebuffer,
    //                    shadowFarQueue, shadowFarFrustumItems, shadowFarList, RenderPasses.ShadowMapFar);
    //                TimerRenderPasses[5].Stop();
    //            });

    //            var mainTask = Task.Run(() => DrawMainPass(depthClear, drawDelta));
    //            TimerRenderPasses[12].Stop();

    //            await Task.WhenAll(shadowMapTaskNear, shadowMapTaskMid, shadowMapTaskFar, mainTask);

    //            TimerRenderPasses[6].Start();
    //            shadowmapCommandList[0].End();
    //            graphicsDevice.SubmitCommands(shadowmapCommandList[0]);
    //            shadowmapCommandList[1].End();
    //            graphicsDevice.SubmitCommands(shadowmapCommandList[1]);
    //            shadowmapCommandList[2].End();
    //            graphicsDevice.SubmitCommands(shadowmapCommandList[2]);
    //            mainPassCommandList.End();
    //            graphicsDevice.SubmitCommands(mainPassCommandList);
    //            TimerRenderPasses[6].Stop();

    //            mainPassCompletionEvent.Release();

    //            TimerRenderPasses[7].Start();
    //            mainCommandList.Begin();
    //            if (RenderContext.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
    //            {
    //                mainCommandList.ResolveTexture(RenderContext.MainSceneColorTexture, RenderContext.MainSceneResolvedColorTexture);
    //            }

    //            //mainCommandList.SetFramebuffer(RenderContext.DuplicatorFramebuffer);
    //            //fbWidth = RenderContext.DuplicatorFramebuffer.Width;
    //            //fbHeight = RenderContext.DuplicatorFramebuffer.Height;
    //            //mainCommandList.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
    //            //mainCommandList.SetViewport(1, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
    //            //mainCommandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
    //            //mainCommandList.SetScissorRect(1, 0, 0, fbWidth, fbHeight);
    //            //DrawRenderPass(RenderPasses.Duplicator, mainCommandList, RenderContext, mainPassList);
    //            mainCommandList.End();
    //            graphicsDevice.SubmitCommands(mainCommandList);
    //            TimerRenderPasses[7].Stop();

    //            TimerRenderPasses[8].Start();
    //            swapchainCommandList.Begin();
    //            swapchainCommandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
    //            fbWidth = graphicsDevice.SwapchainFramebuffer.Width;
    //            fbHeight = graphicsDevice.SwapchainFramebuffer.Height;
    //            swapchainCommandList.SetViewport(0, Viewport ?? new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
    //            swapchainCommandList.SetScissorRect(0, 0, 0, fbWidth, fbHeight);
    //            DrawRenderPass(RenderPasses.SwapchainOutput, swapchainCommandList, RenderContext, mainPassList);
    //            swapchainCommandList.End();
    //            TimerRenderPasses[8].Start();

    //            TimerRenderPasses[9].Start();
    //            graphicsDevice.SubmitCommands(swapchainCommandList);
    //            graphicsDevice.SwapBuffers();
    //            TimerRenderPasses[9].Stop();
    //        }

    //        shutdownEvent.Release();
    //    }

    //    private (Matrix4x4 View, Matrix4x4 Projection) UpdateDirectionalLightMatrices(
    //        GraphicsDevice gd,
    //        Scene scene,
    //        float near,
    //        float far,
    //        uint shadowMapWidth,
    //        out BoundingFrustum lightFrustum)
    //    {
    //        Debug.Assert(scene?.Camera.Value != null);
    //        Debug.Assert(scene?.LightSystem.Value != null);

    //        //Vector3 lightDir = scene.LightSystem.Value.DirectionalLightDirection;
    //        //Vector3 viewDir = scene.Camera.Value.LookAt;
    //        //Vector3 viewPos = scene.Camera.Value.Position;
    //        //Vector3 up = scene.Camera.Value.Up;
    //        //FrustumCorners cameraCorners;

    //        //if (gd.IsDepthRangeZeroToOne)
    //        //{
    //        //    FrustumHelpers.ComputePerspectiveFrustumCorners(
    //        //        ref viewPos,
    //        //        ref viewDir,
    //        //        ref up,
    //        //        scene.Camera.Value.FieldOfView,
    //        //        far,
    //        //        near,
    //        //        scene.Camera.Value.AspectRatio,
    //        //        out cameraCorners);
    //        //}
    //        //else
    //        //{
    //        //    FrustumHelpers.ComputePerspectiveFrustumCorners(
    //        //        ref viewPos,
    //        //        ref viewDir,
    //        //        ref up,
    //        //        scene.Camera.Value.FieldOfView,
    //        //        near,
    //        //        far,
    //        //        scene.Camera.Value.AspectRatio,
    //        //        out cameraCorners);
    //        //}

    //        //// Approach used: http://alextardif.com/
    //        //Vector3 frustumCenter = Vector3.Zero;
    //        //frustumCenter += cameraCorners.NearTopLeft;
    //        //frustumCenter += cameraCorners.NearTopRight;
    //        //frustumCenter += cameraCorners.NearBottomLeft;
    //        //frustumCenter += cameraCorners.NearBottomRight;
    //        //frustumCenter += cameraCorners.FarTopLeft;
    //        //frustumCenter += cameraCorners.FarTopRight;
    //        //frustumCenter += cameraCorners.FarBottomLeft;
    //        //frustumCenter += cameraCorners.FarBottomRight;
    //        //frustumCenter /= 8f;

    //        //float radius = (cameraCorners.NearTopLeft - cameraCorners.FarBottomRight).Length() / 2.0f;
    //        //float texelsPerUnit = shadowMapWidth / (radius * 2.0f);

    //        //Matrix4x4 scalar = Matrix4x4.CreateScale(texelsPerUnit, texelsPerUnit, texelsPerUnit);

    //        //Vector3 baseLookAt = -lightDir;

    //        //Matrix4x4 lookat = Matrix4x4.CreateLookAt(Vector3.Zero, baseLookAt, scene.Camera.Value.Up);
    //        //lookat = scalar * lookat;
    //        //Matrix4x4.Invert(lookat, out Matrix4x4 lookatInv);

    //        //frustumCenter = Vector3.Transform(frustumCenter, lookat);
    //        //frustumCenter.X = (int)frustumCenter.X;
    //        //frustumCenter.Y = (int)frustumCenter.Y;
    //        //frustumCenter = Vector3.Transform(frustumCenter, lookatInv);

    //        //Vector3 lightPos = frustumCenter - (lightDir * radius * 2f);
    //        //Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, frustumCenter, scene.Camera.Value.Up);
    //        Vector3 lightPos = scene.Camera.Value.Position - scene.LightSystem.Value.DirectionalLightDirection * far;
    //        Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, scene.LightSystem.Value.DirectionalLightDirection + scene.Camera.Value.Position, scene.Camera.Value.Up);

    //        Matrix4x4 lightProjection = Matrix4x4Extensions.CreateOrthographic(graphicsDevice.IsClipSpaceYInverted, graphicsDevice.IsDepthRangeZeroToOne,
    //            -far * CascadedShadowMaps.LScale,
    //            far * CascadedShadowMaps.RScale,
    //            -far * CascadedShadowMaps.BScale,
    //            far * CascadedShadowMaps.TScale,
    //            -far * CascadedShadowMaps.NScale,
    //            far * CascadedShadowMaps.FScale);

    //        lightFrustum = new BoundingFrustum(lightView * lightProjection);
    //        return (lightView, lightProjection);
    //    }

    //    private void FillRenderQueue(in RenderQueue queue, in List<Renderable> frustumItems, in List<Renderable> list, Scene scene, BoundingFrustum frustum, Vector3 viewPosition)
    //    {
    //        TimerGetVisibleObjects.Start();
    //        queue.Clear();
    //        frustumItems.Clear();
    //        list.Clear();

    //        scene.GetContainedRenderables(frustum, frustumItems);
    //        queue.AddRange(frustumItems, viewPosition);
    //        queue.Sort();

    //        // TODO: my the hell reverse?? (if not reversing transparency is wrong) also this uses a new list  every time!!!!!!!!!!
    //        list.AddRange(queue);
    //        list.Reverse();
    //        TimerGetVisibleObjects.Stop();
    //    }

    //    private void DrawRenderPass(RenderPasses renderPass, CommandList commandList, RenderContext renderContext, List<Renderable> queue)
    //    {
    //        foreach (var model in queue)
    //        {
    //            if ((model.RenderPasses & renderPass) != 0)
    //            {
    //                model.Render(graphicsDevice, commandList, renderContext, renderPass);
    //            }
    //        }
    //    }
    //}
}

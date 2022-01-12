using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Models;
using NtFreX.BuildingBlocks.Shell;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    //TODO: instancing, veldrid
    struct NullNarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        private readonly IContactEventHandler contactEventHandler;

        public NullNarrowPhaseCallbacks(IContactEventHandler contactEventHandler)
        {
            this.contactEventHandler = contactEventHandler;
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            => pair.A.Mobility == CollidableMobility.Dynamic || pair.B.Mobility == CollidableMobility.Dynamic;

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => true;

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = 0.01f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1);
            contactEventHandler.HandleContact(pair, manifold);
            return true;
        }

        public void Dispose() { }

        public void Initialize(Simulation simulation) { }
    }

    public interface IContactEventHandler
    {
        void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>;
    }
    class NullContactEventHandler : IContactEventHandler
    {
        public void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        {}
    }
    struct NullPoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        //Vector3 gravityWideDt;
        //float linearDampingDt;
        //float angularDampingDt;
        Vector3Wide gravityWideDt;
        Vector<float> linearDampingDt;
        Vector<float> angularDampingDt;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public bool AllowSubstepsForUnconstrainedBodies => true;

        public bool IntegrateVelocityForKinematics => true;

        public void Initialize(Simulation simulation) { }

        //public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
        //{
        //    velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
        //    velocity.Angular = velocity.Angular * angularDampingDt;
        //}

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
            velocity.Angular = velocity.Angular * angularDampingDt;
        }

        public void PrepareForIntegration(float dt)
        {
            const float linearDamping = .003f;
            const float angularDamping = .003f;
            Vector3 gravity = new Vector3(0, -9f, 0);

            //linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - linearDamping, 0, 1), dt));
            //angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - angularDamping, 0, 1), dt));
            //gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
            linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - linearDamping, 0, 1), dt));
            angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - angularDamping, 0, 1), dt));
            gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
        }
    }
    public abstract class Game
    {
        public Stopwatch Stopwatch { get; private set; } = Stopwatch.StartNew();
        public DaeModelImporter DaeModelImporter { get; private set; }
        public ObjModelImporter ObjModelImporter { get; private set; }

        public TextureFactory TextureFactory { get; private set; }
        public GraphicsSystem GraphicsSystem { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public SdlAudioSystem AudioSystem { get; private set; }
        public DisposeCollectorResourceFactory ResourceFactory { get; private set; }
        public ImGuiRenderer UiRenderer { get; private set; }
        public Simulation Simulation { get; private set; }
        public IContactEventHandler ContactEventHandler { get; set; } = new NullContactEventHandler();
        public IShell Shell { get; private set; }
        public ILoggerFactory LoggerFactory { get; private set; }
        public InputHandler InputHandler { get; private set; }

        private readonly BufferPool simulationBufferPool = new BufferPool();
        private CommandList commandList;

        private double? previousRenderingElapsed;
        private double? previousUpdaingElapsed;

        private DebugExecutionTimerSource timerRendering;
        private DebugExecutionTimerSource timerUpdating;
        private DebugExecutionTimerSource timerUpdateInput;
        private DebugExecutionTimerSource timerUpdateUi;
        private DebugExecutionTimerSource timerUpdateApp;
        private DebugExecutionTimerSource timerUpdateGraphics;
        private DebugExecutionTimerSource timerUpdateSimulation;

        public Game(IShell shell, ILoggerFactory loggerFactory)
        {
            shell.GraphicsDeviceCreated += OnGraphicsDeviceCreatedAsync;
            shell.GraphicsDeviceDestroyed += OnGraphicsDeviceDestroyed;
            shell.Rendering += OnRendering;
            shell.Updating += OnUpdating;

            Shell = shell;
            LoggerFactory = loggerFactory;
            TextureFactory = new TextureFactory(this, loggerFactory.CreateLogger<TextureFactory>());
            InputHandler = new InputHandler();

            shell.Resized += () => OnWindowResized();
        }

        protected virtual IContactEventHandler LoadContactEventHandler() => new NullContactEventHandler();
        protected abstract Camera LoadCamera();
        protected abstract void OnRendering(float deleta, CommandList commandList);
        protected abstract void OnUpdating(float delta);
        protected abstract Task LoadResourcesAsync();

        protected virtual void OnWindowResized()
        {
            GraphicsDevice.ResizeMainWindow(Shell.Width, Shell.Height);
            UiRenderer.WindowResized((int)Shell.Width, (int)Shell.Height);
            GraphicsSystem.OnWindowResized((int)Shell.Width, (int)Shell.Height);
        }

        private void OnGraphicsDeviceDestroyed()
        {
            GraphicsDevice.WaitForIdle();
            ResourceFactory.DisposeCollector.DisposeAll();
            GraphicsDevice.Dispose();
            UiRenderer.Dispose();
            AudioSystem.Dispose();
            Simulation.Dispose();
        }

        private async Task OnGraphicsDeviceCreatedAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Swapchain _)
        {
            GraphicsDevice = graphicsDevice;
            ResourceFactory = new DisposeCollectorResourceFactory(resourceFactory);
            UiRenderer = new ImGuiRenderer(GraphicsDevice, GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, (int)Shell.Width, (int)Shell.Height);
            GraphicsSystem = new GraphicsSystem(LoggerFactory, ResourceFactory, LoadCamera());
            AudioSystem = new SdlAudioSystem(GraphicsSystem);
            Simulation = Simulation.Create(simulationBufferPool, new NullNarrowPhaseCallbacks(LoadContactEventHandler()), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 4));
            DaeModelImporter = new DaeModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem);
            ObjModelImporter = new ObjModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem);

            timerRendering = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Render");
            timerUpdating = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update");
            timerUpdateInput = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Input");
            timerUpdateSimulation = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Simulation");
            timerUpdateGraphics = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Graphics");
            timerUpdateUi = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Ui");
            timerUpdateApp = new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update App");
            commandList = ResourceFactory.CreateCommandList();

            await LoadResourcesAsync();
        }

        private void OnUpdating(InputSnapshot inputSnapshot)
        {
            using var timer = new DebugExecutionTimer(timerUpdating);

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var updateDelta = (float)(previousUpdaingElapsed == null ? .00000001f : elapsed - previousUpdaingElapsed.Value);
            previousUpdaingElapsed = elapsed;

            using (var _ = new DebugExecutionTimer(timerUpdateSimulation))
            {
                Simulation.Timestep(updateDelta);
            }
            using (var _ = new DebugExecutionTimer(timerUpdateInput))
            {
                InputHandler.Update(inputSnapshot);
            }
            using (var _ = new DebugExecutionTimer(timerUpdateUi))
            {
                UiRenderer.Update(updateDelta, inputSnapshot);
            }
            using (var _ = new DebugExecutionTimer(timerUpdateGraphics))
            {
                GraphicsSystem.Update(GraphicsDevice, updateDelta, InputHandler);
            }
            using (var _ = new DebugExecutionTimer(timerUpdateApp))
            {
                OnUpdating(updateDelta);
            }
        }

        private void OnRendering()
        {
            using var timer = new DebugExecutionTimer(timerRendering);

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var renderDelta = (float)(previousRenderingElapsed == null ? 0f : elapsed - previousRenderingElapsed.Value);
            previousRenderingElapsed = elapsed;

            commandList.Begin();
            commandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
            commandList.SetFullViewports();

            float depthClear = GraphicsDevice.IsDepthRangeZeroToOne ? 1f : 0f;
            commandList.ClearDepthStencil(depthClear);
            commandList.ClearColorTarget(0, RgbaFloat.Pink);
            
            GraphicsSystem.Draw(commandList);

            if (Shell.IsDebug)
            {
                ImGui.Begin("Debug info");
                ImGui.Text(timerUpdating.Average.TotalMilliseconds + "ms update time");
                ImGui.Text(" - " + timerUpdateSimulation.Average.TotalMilliseconds + "ms update simulation time");
                ImGui.Text(" - " + timerUpdateInput.Average.TotalMilliseconds + "ms read input time");
                ImGui.Text(" - " + timerUpdateUi.Average.TotalMilliseconds + "ms update ui time");
                ImGui.Text(" - " + timerUpdateGraphics.Average.TotalMilliseconds + "ms update graphics time");
                ImGui.Text(" - " + timerUpdateApp.Average.TotalMilliseconds + "ms update app time");
                ImGui.Text(timerRendering.Average.TotalMilliseconds + "ms draw time");
                ImGui.Text(Math.Round(1f / renderDelta) + " FPS");
                ImGui.End();

                ImGui.Begin("Camera");
                ImGui.Text($"Position : {GraphicsSystem.Camera?.Position}");
                ImGui.Text($"LookAt : {GraphicsSystem.Camera?.LookAt}");
                ImGui.End();
            }

            OnRendering(renderDelta, commandList);

            UiRenderer.Render(GraphicsDevice, commandList);
            commandList.End();
            GraphicsDevice.SubmitCommands(commandList);
            GraphicsDevice.SwapBuffers();
            GraphicsDevice.WaitForIdle();
        }
    }
}

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
using NtFreX.BuildingBlocks.Texture;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
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
            pairMaterial.FrictionCoefficient = 1f;
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
    //    Vector3 gravityWideDt;
    //    float linearDampingDt;
    //    float angularDampingDt;
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
            var offset = position - Vector3Wide.Broadcast(Vector3.Zero);
            var distance = offset.Length();
            velocity.Linear = (linearDampingDt * velocity.Linear) - gravityWideDt * offset / Vector.Max(Vector<float>.One, distance * distance * distance);
            velocity.Angular = velocity.Angular * angularDampingDt;
        }

        public void PrepareForIntegration(float dt)
        {
            const float linearDamping = .03f;
            const float angularDamping = .03f;
            Vector3 gravity = new Vector3(0, -9f, 0);

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

        private double[] updateFps = new double[50];
        private int updateFpsIndex = 0;
        private double[] drawFps = new double[50];
        private int drawFpsIndex = 0;

        private double? previousRenderingElapsed;
        private double? previousUpdaingElapsed;

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

        private void OnUpdating(InputSnapshot inputSnapshot)
        {
            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var updateDelta = (float) (previousUpdaingElapsed == null ? .00000001f : elapsed - previousUpdaingElapsed.Value);
            previousUpdaingElapsed = elapsed;

            Simulation.Timestep(updateDelta);
            InputHandler.Update(inputSnapshot);
            UiRenderer.Update(updateDelta, inputSnapshot);
            GraphicsSystem.Update(GraphicsDevice, updateDelta, InputHandler);
            updateFps[updateFpsIndex++] = 1f / updateDelta;
            updateFpsIndex = updateFpsIndex == updateFps.Length ? 0 : updateFpsIndex;
            OnUpdating(updateDelta);
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
            GraphicsSystem = new GraphicsSystem(ResourceFactory, LoadCamera());
            AudioSystem = new SdlAudioSystem(GraphicsSystem);
            Simulation = Simulation.Create(simulationBufferPool, new NullNarrowPhaseCallbacks(LoadContactEventHandler()), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 8));
            DaeModelImporter = new DaeModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem, Simulation);
            ObjModelImporter = new ObjModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem, Simulation);
            commandList = ResourceFactory.CreateCommandList();

            await LoadResourcesAsync();
        }

        protected virtual IContactEventHandler LoadContactEventHandler() => new NullContactEventHandler();
        protected abstract Camera LoadCamera();
        protected virtual void OnWindowResized()
        {
            GraphicsDevice.ResizeMainWindow(Shell.Width, Shell.Height);
            UiRenderer.WindowResized((int)Shell.Width, (int)Shell.Height);
            GraphicsSystem.OnWindowResized((int)Shell.Width, (int)Shell.Height);
        }
        private void OnRendering()
        {
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
                drawFps[drawFpsIndex++] = 1f / renderDelta;
                drawFpsIndex = drawFpsIndex == drawFps.Length ? 0 : drawFpsIndex;

                ImGui.Begin("Debug info");
                ImGui.Text(Math.Round(updateFps.Average()) + " Update FPS");
                ImGui.Text(Math.Round(drawFps.Average()) + " Draw FPS");
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

        protected abstract void OnRendering(float deleta, CommandList commandList);
        protected abstract void OnUpdating(float delta);
        protected abstract Task LoadResourcesAsync();
    }
}

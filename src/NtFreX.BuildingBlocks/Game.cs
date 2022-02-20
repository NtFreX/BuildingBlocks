using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Audio.Sdl2;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Import;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Model.Common;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Shell;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    //TODO: memory leaks in sample game update
    //TODO: app update performance improvement
    //TODO: update graphics time performance
    //TODO: multitheading (create different steps to parallelize: maybe pyhisics independend update, pre phciscs update, post physics update, multitheaded rendering)
    //TODO: text atlas offset wrong when using multile fonts?
    public abstract class Game
    {
        public Stopwatch Stopwatch { get; private set; } = Stopwatch.StartNew();
        public AssimpDaeModelImporter AssimpDaeModelImporter { get; private set; }
        public DaeModelImporter DaeModelImporter { get; private set; }
        public ObjModelImporter ObjModelImporter { get; private set; }
        public TextureFactory TextureFactory { get; private set; }
        public GraphicsSystem GraphicsSystem { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public IAudioSystem? AudioSystem { get; private set; }
        public DisposeCollectorResourceFactory ResourceFactory { get; private set; }
        public Simulation? BepuSimulation { get; private set; }
        public IContactEventHandler BepuContactEventHandler { get; set; } = new NullContactEventHandler();
        public IShell Shell { get; private set; }
        public ILoggerFactory LoggerFactory { get; private set; }
        public InputHandler InputHandler { get; private set; }
        public RenderDoc? RenderDoc { get; private set; }
        public ILogger<Game> Logger { get; private set; }
        public IThreadDispatcher ThreadDispatcher { get; private set; }
        public Scene CurrentScene { get; private set; } = new Scene();

        public AudioSystemType AudioSystemType { get; set; }
        public bool EnableBepuSimulation { get; set; }
        public bool EnableImGui { get; set; }

        private double? previousRenderingElapsed;
        private double? previousUpdaingElapsed;

        private Model.Common.ImGuiRenderer? imGuiRenderable;
        private DebugExecutionTimer timerAudioUpdate;
        private DebugExecutionTimer timerRendering;
        private DebugExecutionTimer timerUpdating;
        private DebugExecutionTimer timerUpdateInput;
        private DebugExecutionTimer timerAfterGraphicsUpdate;
        private DebugExecutionTimer timerBeforeGraphicsUpdate;
        private DebugExecutionTimer timerUpdateGraphics;
        private DebugExecutionTimer timerUpdateSimulation;

        private void Setup(IShell shell, ILoggerFactory loggerFactory)
        {
            shell.GraphicsDeviceCreated += OnGraphicsDeviceCreatedAsync;
            shell.GraphicsDeviceDestroyed += OnGraphicsDeviceDestroyed;
            shell.RenderingAsync += OnRenderingAsync;
            shell.Updating += OnUpdating;

            Shell = shell;
            LoggerFactory = loggerFactory;
            Logger = LoggerFactory.CreateLogger<Game>();
            TextureFactory = new TextureFactory(this, loggerFactory.CreateLogger<TextureFactory>());
            InputHandler = new InputHandler();

            shell.Resized += () => OnWindowResized();

            if (shell.IsDebug)
            {
                if (!RenderDoc.Load(out var renderDoc))
                    Logger.LogWarning("Could not connect to render doc");
                else
                    RenderDoc = renderDoc;
            }
        }

        public static async Task SetupShellAndRunAsync<T>(IShell shell, ILoggerFactory? loggerFactory = null)
            where T : Game, new()
        {
            SetupShell<T>(shell, loggerFactory);
            await shell.RunAsync();
        }

        public static void SetupShell<T>(IShell shell, ILoggerFactory? loggerFactory = null)
            where T: Game, new()
        {
            loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.LoggerFactory.Create(x => x.AddConsole());
            var game = new T();
            game.Setup(shell, loggerFactory);
        }

        // TODO: move camera to scene and rename to get default scene
        protected abstract Camera GetDefaultCamera();
        protected virtual Simulation? LoadBeupSimulation() => Simulation.Create(new BufferPool(), new NullNarrowPhaseCallbacks(new NullContactEventHandler()), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 4));
        protected virtual void BeforeGraphicsSystemUpdate(float delta) { }
        protected virtual void AfterGraphicsSystemUpdate(float delta) { }
        protected virtual Task LoadResourcesAsync() => Task.CompletedTask;

        private void OnWindowResized()
        {
            imGuiRenderable?.WindowResized((int)Shell.Width, (int)Shell.Height);
            GraphicsDevice.ResizeMainWindow(Shell.Width, Shell.Height);
            GraphicsSystem.OnWindowResized((int)Shell.Width, (int)Shell.Height);
        }

        private void OnGraphicsDeviceDestroyed()
        {
            MeshRenderPassFactory.Unload();
            CurrentScene.DestroyAllDeviceObjects();
            GraphicsDevice.WaitForIdle();
            ResourceFactory.DisposeCollector.DisposeAll();
            TextureFactory.Dispose();
            GraphicsDevice.Dispose();
            AudioSystem?.Dispose();
            BepuSimulation?.Dispose();
        }

        private async Task OnGraphicsDeviceCreatedAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Swapchain _)
        {

            if (EnableImGui)
            {
                imGuiRenderable = new Model.Common.ImGuiRenderer((int)Shell.Width, (int)Shell.Height);
                CurrentScene.AddUpdateables(imGuiRenderable);
                CurrentScene.AddFreeRenderables(imGuiRenderable);
            }

            GraphicsDevice = graphicsDevice;
            ThreadDispatcher = new SimpleThreadDispatcher(Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1));
            ResourceFactory = new DisposeCollectorResourceFactory(resourceFactory);
            GraphicsSystem = new GraphicsSystem(LoggerFactory, GraphicsDevice, ResourceFactory, GetDefaultCamera());
            AudioSystem = AudioSystemType == AudioSystemType.Sdl2 ? new SdlAudioSystem(GraphicsSystem) : null;
            BepuSimulation = EnableBepuSimulation ? LoadBeupSimulation() : null;
            DaeModelImporter = new DaeModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem);
            AssimpDaeModelImporter = new AssimpDaeModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem);
            ObjModelImporter = new ObjModelImporter(GraphicsDevice, ResourceFactory, TextureFactory, GraphicsSystem);

            timerAudioUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Audio Update"));
            timerRendering = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Render"));
            timerUpdating = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update"));
            timerUpdateInput = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Input"));
            timerUpdateSimulation = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Simulation"));
            timerUpdateGraphics = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Graphics"));
            timerAfterGraphicsUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game After Graphics Update"));
            timerBeforeGraphicsUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Before Graphics Update"));

            MeshRenderPassFactory.Load(GraphicsDevice, ResourceFactory, Shell.IsDebug);

            // TODO: improve
            var cl = resourceFactory.CreateCommandList();
            cl.Begin();
            CurrentScene.CreateAllDeviceObjects(graphicsDevice, ResourceFactory, GraphicsSystem, cl, new RenderContext { MainSceneFramebuffer = graphicsDevice.MainSwapchain.Framebuffer });
            cl.End();
            graphicsDevice.SubmitCommands(cl);
            cl.Dispose();

            await LoadResourcesAsync();
        }

        private void OnUpdating(InputSnapshot inputSnapshot)
        {
            timerUpdating.Start();

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var updateDelta = (float)(previousUpdaingElapsed == null ? float.MinValue: elapsed - previousUpdaingElapsed.Value);
            previousUpdaingElapsed = elapsed;
                        
            {
                timerUpdateSimulation.Start();
                BepuSimulation?.Timestep(updateDelta, ThreadDispatcher);
                timerUpdateSimulation.Stop();
            }
            
            {
                timerUpdateInput.Start();
                InputHandler.Update(inputSnapshot);
                timerUpdateInput.Stop();
            }
            
            {
                timerBeforeGraphicsUpdate.Start();
                BeforeGraphicsSystemUpdate(updateDelta);
                timerBeforeGraphicsUpdate.Stop();
            }
            
            {
                timerUpdateGraphics.Start();
                GraphicsSystem.Update( updateDelta, InputHandler, CurrentScene);
                timerUpdateGraphics.Stop();
            }
            
            {
                timerAfterGraphicsUpdate.Start();
                AfterGraphicsSystemUpdate(updateDelta);
                timerAfterGraphicsUpdate.Stop();
            }

            {
                timerAudioUpdate.Start();
                AudioSystem?.Update(GraphicsSystem.Camera.Value?.Position.Value);
                timerAudioUpdate.Stop();
            }

            timerUpdating.Stop();
        }

        private async Task OnRenderingAsync()
        {
            timerRendering.Start();

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var renderDelta = (float)(previousRenderingElapsed == null ? 0f : elapsed - previousRenderingElapsed.Value);
            previousRenderingElapsed = elapsed;

            if (Shell.IsDebug && EnableImGui)
            {
                ImGui.Begin("Debug info");
                ImGui.Text(timerUpdating.Source.Average.TotalMilliseconds + "ms update time");
                ImGui.Text(" - " + timerUpdateSimulation.Source.Average.TotalMilliseconds + "ms update simulation time");
                ImGui.Text(" - " + timerUpdateInput.Source.Average.TotalMilliseconds + "ms read input time");
                ImGui.Text(" - " + timerAfterGraphicsUpdate.Source.Average.TotalMilliseconds + "ms before graphics update app time");
                ImGui.Text(" - " + timerUpdateGraphics.Source.Average.TotalMilliseconds + "ms update graphics time");
                ImGui.Text(" - " + timerAfterGraphicsUpdate.Source.Average.TotalMilliseconds + "ms after graphics update app time");
                ImGui.Text(" - " + timerAudioUpdate.Source.Average.TotalMilliseconds + "ms audio time");
                ImGui.Text(timerRendering.Source.Average.TotalMilliseconds + "ms draw time");
                ImGui.Text(Math.Round(1f / renderDelta) + " FPS");
                ImGui.End();

                ImGui.Begin("Camera");
                ImGui.Text($"Position : {GraphicsSystem.Camera.Value?.Position.Value}");
                ImGui.Text($"LookAt : {GraphicsSystem.Camera.Value?.LookAt.Value}");
                ImGui.End();
            }

            await GraphicsSystem.DrawAsync(CurrentScene);

            timerRendering.Stop();
        }
    }
}

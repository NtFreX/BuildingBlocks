using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Audio.Sdl2;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Material;
using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Mesh.Import;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Shell;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;
using NtFreX.BuildingBlocks.Texture.Text;
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
        private const string TimerDebugFormat = "0.0000";

        public IFrameLimitter FrameLimitter { get; set; } = new NullFrameLimitter();
        public Stopwatch Stopwatch { get; private set; } = Stopwatch.StartNew();
        public AssimpDaeModelImporter? AssimpDaeModelImporter { get; private set; }
        public DaeModelImporter? DaeModelImporter { get; private set; }
        public ObjModelImporter? ObjModelImporter { get; private set; }
        public TextureFactory? TextureFactory { get; private set; }
        public GraphicsSystem? GraphicsSystem { get; private set; }
        public GraphicsDevice? GraphicsDevice { get; private set; }
        public IAudioSystem? AudioSystem { get; private set; }
        public DisposeCollectorResourceFactory? ResourceFactory { get; private set; }
        public Simulation? BepuSimulation { get; private set; }
        public IContactEventHandler BepuContactEventHandler { get; set; } = new NullContactEventHandler();
        public IShell? Shell { get; private set; }
        public ILoggerFactory? LoggerFactory { get; private set; }
        public InputHandler InputHandler { get; private set; }
        public RenderDoc? RenderDoc { get; private set; }
        public ILogger<Game>? Logger { get; private set; }
        public IThreadDispatcher ThreadDispatcher { get; private set; }
        public Scene? CurrentScene { get; private set; }
        public TextureSampleCount TextureSampleCount { get; private set; } = TextureSampleCount.Count32;
        public CommandListPool? CommandListPool { get; private set; }
        public RenderContext? RenderContext { get; private set; }

        // TODO: support changing this values after setup if not block changing them
        public AudioSystemType AudioSystemType { get; set; }
        public bool EnableBepuSimulation { get; set; }
        public bool EnableImGui { get; set; }

        private double? previousRenderingElapsed;
        private double? previousUpdaingElapsed;

        private readonly Model.Common.ImGuiRenderer imGuiRenderable;

        private DebugExecutionTimer? timerAudioUpdate;
        private DebugExecutionTimer? timerRendering;
        private DebugExecutionTimer? timerUpdating;
        private DebugExecutionTimer? timerUpdateInput;
        private DebugExecutionTimer? timerAfterGraphicsUpdate;
        private DebugExecutionTimer? timerBeforeGraphicsUpdate;
        private DebugExecutionTimer? timerUpdateGraphics;
        private DebugExecutionTimer? timerUpdateSimulation;

        public Game()
        {
            ThreadDispatcher = new ThreadDispatcher(Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1));
            InputHandler = new InputHandler();

            imGuiRenderable = new Model.Common.ImGuiRenderer(0, 0);
        }

        protected virtual Task SetupAsync(IShell shell, ILoggerFactory loggerFactory)
        {
            if (Shell != null)
                throw new Exception("Cannot initialize more then once");

            Shell = shell;
            Shell.GraphicsDeviceCreated += OnGraphicsDeviceCreatedAsync;
            Shell.GraphicsDeviceDestroyed += OnGraphicsDeviceDestroyed;
            Shell.RenderingAsync += OnRenderingAsync;
            Shell.UpdatingAsync += OnUpdatingAsync;
            shell.Resized += () => OnWindowResized();

            LoggerFactory = loggerFactory;
            Logger = LoggerFactory.CreateLogger<Game>();
            TextureFactory = new TextureFactory(loggerFactory.CreateLogger<TextureFactory>());
            DaeModelImporter = new DaeModelImporter(TextureFactory);
            AssimpDaeModelImporter = new AssimpDaeModelImporter(TextureFactory);
            ObjModelImporter = new ObjModelImporter(TextureFactory);
            AudioSystem = AudioSystemType == AudioSystemType.Sdl2 ? new SdlAudioSystem() : null;
            BepuSimulation = EnableBepuSimulation ? LoadBeupSimulation() : null;

            timerAudioUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Audio Update"), Stopwatch);
            timerRendering = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Render"), Stopwatch);
            timerUpdating = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update"), Stopwatch);
            timerUpdateInput = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Input"), Stopwatch);
            timerUpdateSimulation = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Simulation"), Stopwatch);
            timerUpdateGraphics = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Update Graphics"), Stopwatch);
            timerAfterGraphicsUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game After Graphics Update"), Stopwatch);
            timerBeforeGraphicsUpdate = new DebugExecutionTimer(new DebugExecutionTimerSource(LoggerFactory.CreateLogger<DebugExecutionTimerSource>(), "Game Before Graphics Update"), Stopwatch);

            if (shell.IsDebug)
            {
                if (!RenderDoc.Load(out var renderDoc))
                    Logger.LogWarning("Could not connect to render doc");
                else
                    RenderDoc = renderDoc;
            }

            OnWindowResized();
            return Task.CompletedTask;
        }
        public static async Task SetupShellAndRunAsync<T>(IShell shell, ILoggerFactory? loggerFactory = null)
            where T : Game, new()
        {
            await SetupShellAsync<T>(shell, loggerFactory);
            await shell.RunAsync();
        }
        public static async Task SetupShellAsync<T>(IShell shell, ILoggerFactory? loggerFactory = null)
            where T: Game, new()
        {
            loggerFactory ??= Microsoft.Extensions.Logging.LoggerFactory.Create(x => x.AddConsole());
            var game = new T();
            await game.SetupAsync(shell, loggerFactory);
        }

        //TODO: move to im gui controller?
        public IntPtr GetOrCreateImGuiBinding(TextureView textureView)
            => imGuiRenderable.GetOrCreateImGuiBinding(ResourceFactory ?? throw new Exception("The ResourceFactory is not initialized"), textureView);
        public void RemoveImGuiBinding(TextureView textureView)
            => imGuiRenderable.RemoveImGuiBinding(textureView);

        public async Task ChangeSceneAsync(Scene? scene)
        {
            CurrentScene = scene;

            if (Shell != null && CurrentScene?.Camera.Value != null && CurrentScene.Camera.Value.WindowHeight.Value != Shell.Height && CurrentScene.Camera.Value.WindowWidth.Value != Shell.Width)
            {
                CurrentScene.Camera.Value.WindowHeight.Value = Shell.Height;
                CurrentScene.Camera.Value.WindowWidth.Value = Shell.Width;
            }

            if (CurrentScene != null && EnableImGui)
            {
                Debug.Assert(imGuiRenderable != null);

                CurrentScene.AddUpdateables(imGuiRenderable);
                await CurrentScene.AddFreeRenderablesAsync(imGuiRenderable);
            }

            if (GraphicsDevice != null && CurrentScene != null)
            {
                Debug.Assert(ResourceFactory != null);
                Debug.Assert(RenderContext != null);
                Debug.Assert(CommandListPool != null);

                await CurrentScene.CreateDeviceObjectsAsync(GraphicsDevice, ResourceFactory, RenderContext, CommandListPool);
            }
        }

        protected virtual Simulation? LoadBeupSimulation() => Simulation.Create(new BufferPool(), new NullNarrowPhaseCallbacks(new NullContactEventHandler()), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 4));

        protected virtual Task BeforeGraphicsSystemUpdateAsync(float delta) => Task.CompletedTask;
        protected virtual Task AfterGraphicsSystemUpdateAsync(float delta) => Task.CompletedTask;
        protected virtual void BeforeGraphicsDeviceDestroyed() { }
        protected virtual Task AfterGraphicsDeviceCreatedAsync() => Task.CompletedTask;
        protected virtual void BeforeRenderContextCreated() { }
        protected virtual void AfterRenderContextCreated() { }
        protected virtual void AfterWindowResized() { }
        protected virtual void BeforeGraphicsSystemRender(float delta) { }
        protected virtual void AfterGraphicsSystemRender(float delta) { }

        private void OnWindowResized()
        {
            //TODO: why is this called twice?
            Debug.Assert(Shell != null);

            imGuiRenderable?.WindowResized((int)Shell.Width, (int)Shell.Height);
            if (CurrentScene?.Camera.Value != null)
            {
                CurrentScene.Camera.Value.WindowWidth.Value = Shell.Width;
                CurrentScene.Camera.Value.WindowHeight.Value = Shell.Height;
            }
            if (GraphicsDevice != null)
            {
                Debug.Assert(ResourceFactory != null);

                GraphicsDevice.MainSwapchain.Resize(Shell.Width, Shell.Height);
                RenderContext?.RecreateWindowSizedResources(GraphicsDevice, ResourceFactory);
                GraphicsDevice.WaitForIdle(); // TODO: is this line needed
            }
            AfterWindowResized();
        }

        private void OnGraphicsDeviceDestroyed()
        {
            BeforeGraphicsDeviceDestroyed();

            ResourceLayoutFactory.Dispose();
            ResourceSetFactory.Dispose();
            GraphicsPipelineFactory.Dispose();
            TextIndexBuffer.Instance.DestroyDeviceResources();
            TextAtlas.DestroyAllDeviceResources();
            MeshRenderPassFactory.Unload();
            CurrentScene?.DestroyAllDeviceObjects();
            TextureFactory?.DestroyDeviceResources();

            CommandListPool?.Dispose();
            CommandListPool = null;

            MaterialTextureFactory.Instance.DestroyDeviceResources();

            GraphicsSystem?.DestroyDeviceResources();
            GraphicsSystem = null;

            ResourceFactory?.DisposeCollector.DisposeAll();
            ResourceFactory = null;

            GraphicsDevice?.WaitForIdle();
            GraphicsDevice?.Dispose();
            GraphicsDevice = null;

            RenderContext?.Dispose();
            RenderContext = null;
        }

        private void CreateRenderContext()
        {
            Debug.Assert(GraphicsDevice != null);
            Debug.Assert(ResourceFactory != null);

            BeforeRenderContextCreated();
            RenderContext?.Dispose();
            RenderContext = new RenderContext(GraphicsDevice, ResourceFactory, TextureSampleCount); // TODO: make this configurable
            AfterRenderContextCreated();
        }

        //TODO: remove all async await from main loop and move io responsibility to user? or not because the gain is a few mili sec of cpu but this wont matter?
        // TODO: use swapchain from param?
        private async Task OnGraphicsDeviceCreatedAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Swapchain _)
        {
            if (this.GraphicsDevice != null)
            {
                Debug.Assert(this.GraphicsDevice == graphicsDevice);
                return;
            }

            Debug.Assert(Shell != null);
            Debug.Assert(LoggerFactory != null);

            GraphicsDevice = graphicsDevice;
            ResourceFactory = new DisposeCollectorResourceFactory(resourceFactory);
            GraphicsSystem = new GraphicsSystem(LoggerFactory, GraphicsDevice, ResourceFactory, Stopwatch);
            CommandListPool = new CommandListPool(resourceFactory);
            CreateRenderContext();

            await MaterialTextureFactory.Instance.CreateDeviceResourcesAsync(graphicsDevice, resourceFactory);

            Debug.Assert(RenderContext != null);
            await MeshRenderPassFactory.LoadAsync(GraphicsDevice, ResourceFactory, RenderContext, Shell.IsDebug);

            if (CurrentScene != null)
            {
                await CurrentScene.CreateDeviceObjectsAsync(graphicsDevice, ResourceFactory, RenderContext, CommandListPool);
            }

            await AfterGraphicsDeviceCreatedAsync();
        }

        private async Task OnUpdatingAsync(InputSnapshot inputSnapshot)
        {
            Debug.Assert(timerUpdating != null);
            Debug.Assert(timerUpdateSimulation != null);
            Debug.Assert(timerUpdateInput != null);
            Debug.Assert(timerBeforeGraphicsUpdate != null);
            Debug.Assert(timerUpdateGraphics != null);
            Debug.Assert(timerAfterGraphicsUpdate != null);
            Debug.Assert(timerAudioUpdate != null);
            Debug.Assert(GraphicsSystem != null);
            Debug.Assert(GraphicsDevice != null);

            timerUpdating.Start();

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var updateDelta = (float)(previousUpdaingElapsed == null ? 0: elapsed - previousUpdaingElapsed.Value);
            previousUpdaingElapsed = elapsed;
                        
            {
                timerUpdateSimulation.Start();
                // TODO: this check can be removed if update and update device resources is seperated. update device resources needs to be called when the delta is 0 all other updates not
                if (updateDelta > 0)
                {
                    BepuSimulation?.Timestep(updateDelta, ThreadDispatcher);
                }
                timerUpdateSimulation.Stop();
            }
            
            {
                timerUpdateInput.Start();
                InputHandler.Update(inputSnapshot);
                timerUpdateInput.Stop();
            }

            //TODO: provide an updateNonGraphics method to overwrite? or let the user just spawn it's own tasks?
            //TODO: we can execute until here while the last draw call is still running

            {
                timerBeforeGraphicsUpdate.Start();
                await BeforeGraphicsSystemUpdateAsync(updateDelta);
                timerBeforeGraphicsUpdate.Stop();
            }

            {
                if (CurrentScene != null)
                {
                    timerUpdateGraphics.Start();
                    GraphicsSystem.Update(updateDelta, InputHandler, CurrentScene);
                    timerUpdateGraphics.Stop();
                }
            }
            
            {
                timerAfterGraphicsUpdate.Start();
                await AfterGraphicsSystemUpdateAsync(updateDelta);
                timerAfterGraphicsUpdate.Stop();
            }

            {
                timerAudioUpdate.Start();
                AudioSystem?.Update(CurrentScene?.Camera.Value?.Position.Value);
                timerAudioUpdate.Stop();
            }

            //TODO: submit concurrently at better place (also measure)
            //await CommandListPool?.SubmitAsync(GraphicsDevice);

            timerUpdating.Stop();
        }

        private async Task OnRenderingAsync()
        {
            Debug.Assert(Shell != null);
            Debug.Assert(timerRendering != null);
            Debug.Assert(timerUpdating != null);
            Debug.Assert(timerUpdateSimulation != null);
            Debug.Assert(timerUpdateInput != null);
            Debug.Assert(timerAfterGraphicsUpdate != null);
            Debug.Assert(timerUpdateGraphics != null);
            Debug.Assert(timerAudioUpdate != null);
            Debug.Assert(GraphicsSystem != null);
            Debug.Assert(RenderContext != null);

            timerRendering.Start();

            var elapsed = Stopwatch.Elapsed.TotalSeconds;
            var renderDelta = (float)(previousRenderingElapsed == null ? 0f : elapsed - previousRenderingElapsed.Value);
            previousRenderingElapsed = elapsed;

            if (Shell.IsDebug && EnableImGui)
            {
                ImGui.Begin("Debug info");
                ImGui.Text(timerUpdating.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms update time");
                ImGui.Text(" - " + timerUpdateSimulation.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms update simulation time");
                ImGui.Text(" - " + timerUpdateInput.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms read input time");
                ImGui.Text(" - " + timerAfterGraphicsUpdate.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms before graphics update app time");
                ImGui.Text(" - " + timerUpdateGraphics.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms update graphics time");
                ImGui.Text(" - " + timerAfterGraphicsUpdate.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms after graphics update app time");
                ImGui.Text(" - " + timerAudioUpdate.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms audio time");
                ImGui.Text(timerRendering.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms draw time");
                ImGui.Text(" - " + GraphicsSystem.TimerGetVisibleObjects.Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms " + GraphicsSystem.TimerGetVisibleObjects.Source.Name);
                for(var i = 0; i < GraphicsSystem.TimerRenderPasses.Length; i++)
                {
                    ImGui.Text(" - " + GraphicsSystem.TimerRenderPasses[i].Source.AverageMilliseconds.ToString(TimerDebugFormat) + "ms " + GraphicsSystem.TimerRenderPasses[i].Source.Name);
                }
                ImGui.Text(Math.Round(1f / renderDelta) + " FPS");
                ImGui.End();

                ImGui.Begin("Camera");
                ImGui.Text($"Position : {CurrentScene?.Camera.Value?.Position.Value}");
                ImGui.Text($"LookAt : {CurrentScene?.Camera.Value?.LookAt.Value}");
                ImGui.End();
            }

            BeforeGraphicsSystemRender(renderDelta);
            if (CurrentScene?.Camera.Value != null)
                await GraphicsSystem.DrawAsync(CurrentScene, RenderContext);
            AfterGraphicsSystemRender(renderDelta);

            timerRendering.Stop();

            await FrameLimitter.LimitAsync(renderDelta);
        }
    }
}

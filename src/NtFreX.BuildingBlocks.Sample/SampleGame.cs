using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Common;
using NtFreX.BuildingBlocks.Mesh.Import;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Model.Behaviors;
using NtFreX.BuildingBlocks.Model.Common;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Shell;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture;
using NtFreX.BuildingBlocks.Texture.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;

namespace NtFreX.BuildingBlocks.Sample
{
    //TODO: multithreading and  tasks!!!!!!!!!!!!!!
    //public struct ParticleInfo
    //{
    //    public Vector3 Position;
    //    public Vector3 Scale;
    //    public Vector3 Velocity;
    //    public Vector4 Color;

    //    public ParticleInfo(Vector3 position, Vector3 scale, Vector3 velocity, Vector4 color)
    //    {
    //        Position = position;
    //        Scale = scale;
    //        Velocity = velocity;
    //        Color = color;
    //    }
    //}
    //TODO: fix this make this work delete this change this to spawn textures?
    //public class ParticleSystem
    //{
    //    public const uint MaxParticles = 100000;

    //    private readonly Pipeline computePipeline;
    //    private readonly ResourceSet particleBufferResourceSet;
    //    private readonly ResourceSet infoBufferResourceSet;
    //    private readonly Pipeline graphicsPipeline;
    //    private readonly ResourceSet graphicsParticleResourceSet;
    //    private readonly ResourceSet graphicsInfoResourceSet;
    //    private readonly ResourceSet projectionViewWorldResourceSet;
    //    private readonly ParticleInfo[] particles;

    //    public ParticleSystem(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader computeShader, Shader[] shaders, ParticleInfo[] particles)
    //    {
    //        if (particles.Length > MaxParticles)
    //            throw new Exception($"To many particles, only {MaxParticles} are supported");

    //        var particleBuffer = resourceFactory.CreateBuffer(new BufferDescription(
    //            (uint)(Unsafe.SizeOf<ParticleInfo>() * particles.Length),
    //            BufferUsage.StructuredBufferReadWrite,
    //            (uint)Unsafe.SizeOf<ParticleInfo>()));

    //        var infoBuffer = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

    //        var particleComputeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
    //            new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute)));

    //        var infoComputeLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
    //            new ResourceLayoutElementDescription("InfoBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

    //        var computePipelineDesc = new ComputePipelineDescription(
    //            computeShader,
    //            new[] { particleComputeLayout, infoComputeLayout },
    //            1, 1, 1);

    //        computePipeline = resourceFactory.CreateComputePipeline(ref computePipelineDesc);
    //        particleBufferResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(particleComputeLayout, particleBuffer));
    //        infoBufferResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(infoComputeLayout, infoBuffer));

    //        var particleVertexLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
    //            new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex)));

    //        var infoVertexLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
    //            new ResourceLayoutElementDescription("InfoBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

    //        var projectionViewWorldLayout = ResourceLayoutFactory.GetProjectionViewWorldLayout(resourceFactory);

    //        var shaderSet = new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), shaders);

    //        var  particleDrawPipelineDesc = new GraphicsPipelineDescription(
    //            BlendStateDescription.SingleOverrideBlend,
    //            DepthStencilStateDescription.Disabled,
    //            RasterizerStateDescription.Default,
    //            PrimitiveTopology.PointList,
    //            shaderSet,
    //            new[] { particleVertexLayout, infoVertexLayout, projectionViewWorldLayout },
    //            graphicsDevice.SwapchainFramebuffer.OutputDescription);

    //        var worldBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
    //        graphicsDevice.UpdateBuffer(worldBuffer, 0, Matrix4x4.Identity);

    //        graphicsPipeline = resourceFactory.CreateGraphicsPipeline(ref particleDrawPipelineDesc);
    //        graphicsParticleResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
    //            particleVertexLayout,
    //            particleBuffer));
    //        graphicsInfoResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
    //            infoVertexLayout,
    //            infoBuffer));
    //        projectionViewWorldResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(projectionViewWorldLayout, graphicsSystem.Camera.ProjectionBuffer, graphicsSystem.Camera.ViewBuffer, worldBuffer));

    //        var cl = resourceFactory.CreateCommandList();
    //        cl.Begin();
    //        cl.UpdateBuffer(infoBuffer, 0, new Vector4(particles.Length, 0, 0, 0));
    //        cl.UpdateBuffer(particleBuffer, 0, particles);
    //        cl.End();
            
    //        graphicsDevice.SubmitCommands(cl);
    //        graphicsDevice.WaitForIdle();
    //        this.particles = particles;
    //    }

    //    public void Draw(CommandList cl)
    //    {
    //        cl.SetPipeline(computePipeline);
    //        cl.SetComputeResourceSet(0, particleBufferResourceSet);
    //        cl.SetComputeResourceSet(1, infoBufferResourceSet);
    //        cl.Dispatch((uint)particles.Length, 1, 1);

    //        cl.SetPipeline(graphicsPipeline);
    //        cl.SetGraphicsResourceSet(0, graphicsParticleResourceSet);
    //        cl.SetGraphicsResourceSet(1, graphicsInfoResourceSet);
    //        cl.SetGraphicsResourceSet(2, projectionViewWorldResourceSet);
    //        cl.Draw((uint)particles.Length, 1, 0, 0);
    //    }
    //}
    public class CenterQubeComponent
    {
        private MeshRenderer[] centerQubes = Array.Empty<MeshRenderer>();

        public CenterQubeComponent(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, Scene scene, TextureView blueTexture)
        {
            //TODO: why are they not drawn?
            var qubeSideLength = .5f;
            centerQubes = new MeshRenderer[] {
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = Vector3.Zero }, sideLength: qubeSideLength),

                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, -4) }, texture: blueTexture, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, -3) }, sideLength: qubeSideLength, blue: 1f),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, -2) }, sideLength: qubeSideLength, green: 1f),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, -1) }, sideLength: qubeSideLength, red: 1f),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, 1) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, 2) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, 3) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 0, 4) }, sideLength: qubeSideLength),

                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, -4, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, -3, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, -2, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, -1, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 1, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 2, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 3, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(0, 4, 0) }, sideLength: qubeSideLength),

                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-4, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-3, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-2, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-1, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(1, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(2, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(3, 0, 0) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(4, 0, 0) }, sideLength: qubeSideLength),

                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-4, -4, -4) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-3, -3, -3) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-2, -2, -2) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(-1, -1, -1) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(1, 1, 1) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(2, 2, 2) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(3, 3, 3) }, sideLength: qubeSideLength),
                QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = new Vector3(4, 4, 4) }, sideLength: qubeSideLength),
            };
            scene.AddUpdateables(centerQubes.Select(x => new BepuPhysicsCollidableBehavoir<Box>(simulation, x)).ToArray());
            scene.AddCullRenderables(centerQubes);
        }

        public void SetOpacity(float value)
        {
            foreach (var model in centerQubes)
            {
                model.MeshBuffer.Material.Value = model.MeshBuffer.Material.Value == null ? new MaterialInfo { Opacity = value } : model.MeshBuffer.Material.Value.Value with { Opacity = value };
            }
        }
    }

    public class SunComponent
    {
        private readonly LightSystem lightSystem;
        private readonly MeshRenderer sun;

        public Vector3 Position => sun.Transform.Value.Position;

        public const float SunDistance = 2000f;
        public float SunYawn { get; set; } = 0f;
        public float sunPitch { get; set; } = 0f;
        
        public SunComponent(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, LightSystem lightSystem, Scene scene)
        {
            this.lightSystem = lightSystem;

            sun = SphereModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: new Transform { Position = Vector3.Zero }, red: 1f, green: 1, alpha: 1f, radius: 25f, sectorCount: 25, stackCount: 25);
            scene.AddFreeRenderables(sun);
            scene.AddUpdateables(sun);
        }

        // todo: how to save mesh / recreate mesh
        //public void CreateDeviceResources()
        //{

        //}

        public void Update(float delta)
        {
            var sunSpeed = sun.Transform.Value.Position.Y < 0 ? 0.4f : 0.1f;

            sunPitch += sunSpeed * delta;

            var rotation = Quaternion.CreateFromYawPitchRoll(SunYawn, sunPitch, 0f);
            var lightPos = Vector3.Transform(Vector3.UnitZ, rotation) * SunDistance;
            var brightness = Math.Min(Math.Max((lightPos.Y + (SunDistance / 5)) / SunDistance, 0.05f), .8f);
            lightSystem.AmbientLight = new Vector3(brightness);
            
            sun.Transform.Value = sun.Transform.Value with { Position = lightPos };
        }
    }

    public class SampleGame : Game
    {
        private CenterQubeComponent centerQubeComponent;
        private SunComponent sunComponent;

        private const string dashRunner = @"resources/audio/Dash Runner.wav";
        private const string detective = @"resources/audio/8-bit Detective.wav";

        //private MeshRenderer rotatingQube;
        //private MeshRenderer[]? goblin;
        //private MeshRenderer[]? dragon;

        //private TextureView? emptyTexture;
        //private ParticleSystem particleSystem;

        private readonly Font[] font;

        //private long elapsedMilisecondsSincePhyicsObjectAdd = 0;
        private Stopwatch stopwatch = Stopwatch.StartNew();


        private DeviceBufferPool deviceBufferPool = new DeviceBufferPool(128);
        private CommandListPool commandListPool;
        private TextModelBuffer textModelBuffer;

        private MovableCamera movableCamera;
        //private ThirdPersonCamera thirdPersonCamera;

        //private List<Model> physicsModels = new List<Model>();
        //private List<Car> cars = new List<Car>();

        //private readonly CollidableProperty<SubgroupCollisionFilter> collidableProperties = new CollidableProperty<SubgroupCollisionFilter>();

        public SampleGame()
        {
            EnableImGui = Shell.IsDebug;
            AudioSystemType = AudioSystemType.Sdl2;
            //EnableBepuSimulation = true;
            //font = SystemFonts.Families.Select(x => x.CreateFont(48, FontStyle.Regular)).ToArray();
        }

        //protected override Simulation LoadSimulation() => Simulation.Create(new BepuUtilities.Memory.BufferPool(), new SubgroupFilteredCallbacks(new SampleContactEventHandler(this, AudioSystem), collidableProperties), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 4));

        //protected override void BeforeGraphicsSystemUpdate(float delta)
        //{
        //    if (InputHandler.IsKeyDown(Key.F9))
        //    {
        //        GraphicsSystem.Camera.Value = movableCamera;
        //    }
        //    if (InputHandler.IsKeyDown(Key.F10))
        //    {
        //        var carToFollow = cars[Standard.Random.GetRandomNumber(0, cars.Count)];
        //        thirdPersonCamera.Offset = Vector3.Transform(thirdPersonCamera.Up.Value * 8f + thirdPersonCamera.Forward * -20f, carToFollow.Transform.Rotation);
        //        thirdPersonCamera.LookAtOffset = Vector3.Transform(thirdPersonCamera.Up.Value * -2f + thirdPersonCamera.Forward * 30f, carToFollow.Transform.Rotation);
        //        thirdPersonCamera.Model = carToFollow.Models.First();
        //        thirdPersonCamera.Forward = carToFollow.Configuration.DesiredForward;
        //        GraphicsSystem.Camera.Value = thirdPersonCamera;
        //    }
        //}
        protected override void AfterGraphicsSystemUpdate(float delta)
        {
            base.AfterGraphicsSystemUpdate(delta);

            //if (InputHandler.IsKeyDown(Key.Delete))
            //{
            //    foreach (var car in cars)
            //    {
            //        car.Reset();
            //    }
            //}

            //foreach (var car in cars)
            //{
            //    car.Update(InputHandler, delta);
            //}

            //var text = stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " total seconds elapsed";
            //textModelBuffer.Write(text.Select((item, index) => new TextData(font[index], text[index].ToString(), new RgbaFloat(Standard.Random.GetRandomNumber(0f, 1f), Standard.Random.GetRandomNumber(0f, 1f), Standard.Random.GetRandomNumber(0f, 1f), 1))).ToArray());

            //commandListPool.Submit(GraphicsDevice);

            //TODO: this is a memory leak
            //if (elapsedMilisecondsSincePhyicsObjectAdd + 100 < stopwatch.ElapsedMilliseconds)
            //{
            //    var creationInfo = new ModelCreationInfo { Position = Standard.Random.Noise(new Vector3(10, 5, -25), 0.3f), Rotation = Quaternion.CreateFromYawPitchRoll(Standard.Random.GetRandomNumber(-10f, 10f), Standard.Random.GetRandomNumber(-10f, 10f), Standard.Random.GetRandomNumber(-10f, 10f)) };
            //    var material = new MaterialInfo { DiffuseColor = new Vector4(Standard.Random.GetRandomNumber(0.3f, 1f), Standard.Random.GetRandomNumber(0.3f, 1f), Standard.Random.GetRandomNumber(0.3f, 1f), 1f), Opacity = Standard.Random.GetRandomNumber(0.3f, 1f) };
            //    var velocity = new BodyVelocity(Standard.Random.GetRandomVector(-1f, 1f), Standard.Random.GetRandomVector(-2f, 2f));
            //    if (Standard.Random.GetRandomNumber(0, 2) == 0)
            //    {
            //        physicsModels.Add(
            //            SphereModel.Create(
            //                GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, creationInfo: creationInfo,
            //                material: material, radius: Standard.Random.GetRandomNumber(0.3f, 1.2f), texture: emptyTexture, sectorCount: 25, stackCount: 25, deviceBufferPool: deviceBufferPool)
            //            .AddBehavoirs(body =>
            //                    new BepuPhysicsCollidableBehavoir<Sphere>(Simulation, body, bodyType: BepuPhysicsBodyType.Dynamic, velocity: velocity)));
            //    }
            //    else
            //    {
            //        physicsModels.Add(QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, creationInfo: creationInfo,
            //            material: material, sideLength: Standard.Random.GetRandomNumber(0.3f, 1.2f), texture: emptyTexture, deviceBufferPool: deviceBufferPool).AddBehavoirs(x =>
            //                    new BepuPhysicsCollidableBehavoir<Box>(Simulation, x, bodyType: BepuPhysicsBodyType.Dynamic, velocity: velocity)));
            //    }

            //    GraphicsSystem.AddModels(physicsModels.Last());

            //    while (physicsModels.Count > 500)
            //    {
            //        var model = physicsModels.First();
            //        GraphicsSystem.RemoveModels(model);
            //        physicsModels.RemoveAt(0);
            //        model.Dispose();
            //    }

            //    elapsedMilisecondsSincePhyicsObjectAdd = stopwatch.ElapsedMilliseconds;
            //}


            //sunComponent.Update(delta);
            //centerQubeComponent.SetOpacity(sunComponent.Position.Y / SunComponent.SunDistance);


            //var lights = new List<PointLightInfo>(new[] { new PointLightInfo { Color = new Vector3(.02f, 0, 0), Intensity = Standard.Random.GetRandomNumber(0.05f, 0.2f), Range = 25f, Position = Vector3.Zero } });
            //if (sun.Position.Value.Y >= -10) 
            //{
            //    lights.Add(new PointLightInfo { Color = new Vector3(.02f, Math.Min(Math.Max(brightness, .002f), .02f), 0), Intensity = 0.2f, Range = sunDistance * 2, Position = lightPos });
            //}
            //GraphicsSystem.LightSystem.SetPointLights(lights.ToArray());


            ////TODO: why does transparency not work anymore???
            //rotatingQube.Rotation.Value = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateRotationX(sunPitch) * Matrix4x4.CreateRotationY(sunPitch));


            //foreach (var model in goblin!.Concat(dragon!))
            //{
            //    model.MeshBuffer.FillMode.Value = sun.Position.Value.Z > 0 ? PolygonFillMode.Wireframe : PolygonFillMode.Solid;
            //}
        }

        protected override Camera GetDefaultCamera() 
        {
            if (movableCamera == null)
                movableCamera = new MovableCamera(GraphicsDevice, ResourceFactory, Shell.Width, Shell.Height);
            return movableCamera;            
        }

        protected override async Task LoadResourcesAsync()
        {
            await base.LoadResourcesAsync();
            //commandListPool = new CommandListPool(ResourceFactory);

            //PlayLoadingAudio();

            //emptyTexture = TextureFactory.GetEmptyTexture(TextureUsage.Sampled);
            //var stoneTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/spnza_bricks_a_diff.png", TextureUsage.Sampled);
            //var blueTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/app.png", TextureUsage.Sampled);

            //LoadParticleSystem();

            {
                //MeshRenderPassFactory.RenderPasses.Add(new VertexPositionColorNormalTextureMeshRenderPass(ResourceFactory, ApplicationContext.IsDebug));
            }

            //var vertexShaderDesc = new ShaderDescription(
            //    ShaderStages.Vertex,
            //    File.ReadAllBytes("resources/shaders/basic.vert"),
            //    "main", ApplicationContext.IsDebug);
            //var fragmentShaderDesc = new ShaderDescription(
            //    ShaderStages.Fragment,
            //    File.ReadAllBytes("resources/shaders/basic.frag"),
            //    "main", ApplicationContext.IsDebug);

            //var shaders = ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            //MeshRenderPassFactory.RenderPasses.Add(new VertexPositionColorNormalTextureStandardRenderPass(shaders));

            //TextAtlas.Load(GraphicsDevice, ResourceFactory, font.Take(10).ToArray(), "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,.;:-_+*/=?^'()[]{} ");

            //CurrentScene.AddCullRenderables(TextModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem,
            //        SystemFonts.Find("Arial").CreateFont(22), string.Join(Environment.NewLine, Enumerable.Repeat("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,.;:-_+*/=?^'()[]{} ", 10)), RgbaFloat.Black,
            //        transform: new Transform { Position = new Vector3(50, 50, -50), Scale = Vector3.One * 0.05f }).ToArray());

            //GraphicsSystem.AddModels(TextModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem,
            //        SystemFonts.Find("Arial").CreateFont(40), $"Welcome to the game!", RgbaFloat.Black, shaders,
            //        creationInfo: new ModelCreationInfo { Position = new Vector3(50, 50, 50) })
            //        .AddBehavoirs(model => new AlwaysFaceCameraBehavior(model, Matrix4x4.CreateRotationX(MathHelper.ToRadians(90))))
            //        .AddBehavoirs(model => new GrowWhenFarFromCameraBehavoir(model, .0003f))
            //        .ToArray());

            //var textureShaders = ResourceFactory.CreateFromSpirv(
            //     new ShaderDescription(
            //        ShaderStages.Vertex,
            //        File.ReadAllBytes("resources/shaders/ui.vert"),
            //        "main", ApplicationContext.IsDebug),
            //    new ShaderDescription(
            //        ShaderStages.Fragment,
            //        File.ReadAllBytes("resources/shaders/ui.frag"),
            //        "main", ApplicationContext.IsDebug));
            //textModelBuffer = new TextModelBuffer(CurrentScene, GraphicsDevice, ResourceFactory, GraphicsSystem, deviceBufferPool, commandListPool);
            //textModelBuffer.SetTransform(new Vector3(-20, 0, -20), Vector3.One * 0.01f, Matrix4x4.Identity);


            //var qube = QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, creationInfo: new ModelCreationInfo { Position = new Vector3(20, 20, 20) }, texture: emptyTexture, sideLength: 1, name: "physicsWireframe")
            //    .AddBehavoirs(x => new BepuPhysicsCollidableBehavoir<Box>(Simulation, x, bodyType: BepuPhysicsBodyType.Dynamic));
            //qube.FillMode.Value = PolygonFillMode.Wireframe;
            //GraphicsSystem.AddModels(qube);

            //rotatingQube = QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, sideLength: 3, creationInfo: new ModelCreationInfo { Position = new Vector3(6, 6, 3) }, texture: stoneTexture).AddBehavoirs(x => new BepuPhysicsCollidableBehavoir<Box>(Simulation, x));

            //{
            //    var mesh = await DaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"resources/models/chinesedragon.dae");
            //    var data = MeshDeviceBuffer.Create(GraphicsDevice, ResourceFactory, mesh[0], textureView: emptyTexture);
            //    GraphicsSystem.AddModels(Enumerable.Range(0, 100).Select(x => new Model(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, data, creationInfo: new ModelCreationInfo { Position = Vector3.One * x * 2 + Vector3.UnitY * 50 }, name: $"goblin{x}")).ToArray());
            //}


            //LoadInstanced();

            //await LoadDaeModelAsync();

            //var level001Meshes = await ObjModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\level001.obj");
            //var level001Models = level001Meshes.Select(provider => Model.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, provider, new ModelCreationInfo { Position = new Vector3(-50, -980, -50) }, textureView: emptyTexture, name: "level001", deviceBufferPool)).ToArray();
            //var level001Shape = level001Meshes.CombineVertexPosition32Bit().GetPhysicsMesh(Simulation.BufferPool);
            //var level001ColliderBehavoir = new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(Simulation, level001Models, shape: level001Shape);
            //level001Models.First().AddBehavoirs(level001ColliderBehavoir);
            //GraphicsSystem.AddModels(level001Models);

            //await LoadFloorAsync(stoneTexture);
            //await LoadRacingTracksAsync();
            //await LoadCarsAsync(emptyTexture);
            //await LoadLargeModelsAsync();

            //centerQubeComponent = new CenterQubeComponent(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, CurrentScene, blueTexture);
            //sunComponent = new SunComponent(GraphicsDevice, ResourceFactory, GraphicsSystem, GraphicsSystem.LightSystem, CurrentScene);

            LoadXYZLineModels();
            CreateSkybox();
            //LoadPhysicObjects();
            LoadFloor();

            CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\level_2902.dae")); 
            CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\wall.dae"));
            
            //CurrentScene.AddCullRenderables(await AssimpDaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\cart_level002.dae", new ModelLoadOptions {  Transform = new Transform(position: new Vector3(-1000, 0, -1000)) }));
            //CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\cart_level002.dae", new ModelLoadOptions { Transform = new Transform(position: new Vector3(1000, 0, 1000)) }));

            //await CreateAnimatedModelAsync(@"D:\projects\veldrid-samples\assets\models\goblin.dae"); //@"C:\Users\FTR\Documents\Space Station Scene.dae");

            //goblin = await AssimpDaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, -15), Scale = new Vector3(.001f) }, shaders, @"resources/models/goblin.dae");
            //dragon = await AssimpDaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, 15) }, shaders, @"resources/models/chinesedragon.dae");

            //CurrentScene.AddCullRenderables(sun);
            //CurrentScene.AddCullRenderables(goblin);
            //CurrentScene.AddCullRenderables(dragon);

            DrawBoundingBoxes();

            //thirdPersonCamera = new ThirdPersonCamera(GraphicsDevice, ResourceFactory, Shell.Width, Shell.Height, new Vector3(0, 0, -1), new Vector3(0, 1, 3));

            //AudioSystem.StopAll();
            //AudioSystem.PlaceWav(detective, loop: true, position: Vector3.Zero, intensity: 100f);

            //await CreateAndSaveLoadModelAsync();
        }

        //protected override void OnRendering(float deleta, CommandList commandList) 
        //{
        //    //particleSystem.Draw(commandList);
        //}

        private async Task CreateAnimatedModelAsync(string path)
        {
            var goblinModels = await AssimpDaeModelImporter.ModelFromFileAsync(path);
            foreach (var model in goblinModels)
            {
                if (model.MeshBuffer.Material.Value != null)
                    model.MeshBuffer.Material.Value = model.MeshBuffer.Material.Value.Value with { Opacity = 1f };

                // TODO: support cull renderable for animations?
                var animation = model.MeshBuffer.BoneAnimationProviders?.FirstOrDefault();
                if (animation != null)
                {
                    animation.IsRunning = true;
                    CurrentScene.AddUpdateables(model);
                }
                CurrentScene.AddFreeRenderables(model);
            }
        }
        private void CreateSkybox()
        {
            // TODO: create device resource pattern
            var cl = ResourceFactory.CreateCommandList();
            cl.Begin();
            CurrentScene.AddFreeRenderables(new SkyboxRenderer(
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_ft.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_bk.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_lf.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_rt.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_up.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_dn.png"),
                GraphicsDevice, ResourceFactory, GraphicsSystem, cl, new RenderContext() { MainSceneFramebuffer = GraphicsDevice.SwapchainFramebuffer },
                Shell.IsDebug));
            cl.End();
            GraphicsDevice.SubmitCommands(cl);
            cl.Dispose();
        }

        private async Task CreateAndSaveLoadModelAsync()
        {
            var mesh = new MeshDataProvider<VertexPosition, Index16>(
                new VertexPosition[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, Vector3.One },
                new Index16[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                PrimitiveTopology.TriangleList);

            var filePath = "car.model";
            File.Delete(filePath);
            using (var writer = File.OpenWrite(filePath))
            {
                await mesh.SaveAsync(writer);
            }
            using (var reader = File.OpenRead(filePath))
            {
                var loadedMesh = await MeshDataProvider<VertexPosition, Index16>.LoadAsync(reader);
                loadedMesh.MutateTo32BitIndex();
            }
        }

        private void DrawBoundingBoxes()
        {
            //TODO: why are they not drawn?
            foreach (var model in CurrentScene.CullRenderables)
            {
                var boundingBox = BoundingBoxModel.CreateBoundingBoxModel(GraphicsDevice, ResourceFactory, GraphicsSystem, model);
                boundingBox.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;
                boundingBox.MeshBuffer.Material.Value = new MaterialInfo();
                CurrentScene.AddCullRenderables(boundingBox);
            }
        }

        private void LoadFloor()
        {
            // TODO: add all models as updatables???
            var plane = PlaneModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem,
                    rows: 200, columns: 200, texture: TextureFactory.GetDefaultTexture(TextureUsage.Sampled), material: new MaterialInfo(shininessStrength: .001f),
                    transform: new Transform { Position = GraphicsSystem.Camera.Value.Up.Value * -1000f, Scale = Vector3.One * 100f },
                    name: "floor", physicsBufferPool: BepuSimulation?.BufferPool);
            if(BepuSimulation != null)
                CurrentScene.AddUpdateables(new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, plane));
            CurrentScene.AddCullRenderables(plane);
        }

        private void LoadInstanced()
        {
            for (var i = 0; i < 3; i++)
            {
                var convertingMesh = QubeModel.CreateMesh();
                convertingMesh.Instances = Enumerable
                    .Range(0, 1000)
                    .Select(x => Enumerable
                        .Range(0, 1000)
                        .Select(z => new InstanceInfo
                        {
                            Position = Standard.Random.Noise(new Vector3(x * 2, 0, z * 2), .2f),
                            Rotation = Standard.Random.GetRandomVector(0f, 180f),
                            Scale = Standard.Random.GetRandomVector(0.5f, 1.5f)
                        }))
                    .SelectMany(x => x)
                    .ToArray();

                var data = MeshDeviceBuffer.Create(GraphicsDevice, ResourceFactory, convertingMesh);
                var model = new MeshRenderer(GraphicsDevice, ResourceFactory, GraphicsSystem, data, transform: new Transform { Position = new Vector3(30, -10 + i, 0) }, name: "qubeInstanced");
                CurrentScene.AddCullRenderables(model);
            }
        }


        private void LoadXYZLineModels()
        {
            var lineLength = 10000f;
            CurrentScene.AddCullRenderables(
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, Vector3.UnitX * lineLength, transform: new Transform { Position = Vector3.Zero }, red: 1f, name: "lineXPositive"),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, -Vector3.UnitX * lineLength, transform: new Transform { Position = Vector3.Zero }, red: .5f, name: "lineXNegative"),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, Vector3.UnitY * lineLength, transform: new Transform { Position = Vector3.Zero }, green: 1f, name: "lineYPositive"),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, -Vector3.UnitY * lineLength, transform: new Transform { Position = Vector3.Zero }, green: .5f, name: "lineYNegative"),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, Vector3.UnitZ * lineLength, transform: new Transform { Position = Vector3.Zero }, blue: 1f, name: "linZPositive"),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Vector3.Zero, -Vector3.UnitZ * lineLength, transform: new Transform { Position = Vector3.Zero }, blue: .5f, name: "lineZNegative"));
        }

        private async Task LoadDaeModelAsync()
        {
            //TODO: make this work
            var daeMeshProvider = await DaeFileReader.BinaryMeshFromFileAsync(@"C:\Users\FTR\Documents\tunnel.dae");
            var definedMeshProviders = daeMeshProvider.Meshes.Select(provider => provider
                .Define<VertexPositionNormalTextureColor, Index32>(data => VertexPositionNormalTextureColor.Build(data, provider.VertexLayout))
                .MutateVertices(vertex => new VertexPositionNormalTextureColor(Vector3.Transform(vertex.Position, provider.Transform * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90))), vertex.Color, vertex.TextureCoordinate, vertex.Normal)))
                .ToArray();//.Combine();

            var flattenedProviders = definedMeshProviders.FlattenMeshDataProviders(daeMeshProvider.Instaces);
            var models = flattenedProviders.Select(definedMeshProvider => MeshRenderer.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, definedMeshProvider, new Transform { Position = new Vector3(0, 500, 0), Scale = Vector3.One * 20 }, textureView: TextureFactory.GetDefaultTexture(TextureUsage.Sampled))).ToArray();

            CurrentScene.AddCullRenderables(await AssimpDaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\tunnel.dae", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(500, 1000, 0), Scale = Vector3.One * 20, Rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)) } }));


            //var triangles = daeMeshProvider.SelectMany(x => x.GetTriangles()).ToArray();
            //var triangleMeshProvider = new MeshDataProvider<VertexPositionColorNormalTexture, Index32>(
            //    triangles.SelectMany(x => new[] { x.A, x.B, x.C }).Select(x => new VertexPositionColorNormalTexture(x, RgbaFloat.Blue)).ToArray(), Enumerable.Range(0, triangles.Length * 3).Select(x => (Index32)x).ToArray(), PrimitiveTopology.TriangleList);
            //var colliderModel = Model.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, triangleMeshProvider, textureView: emptyTexture);
            //colliderModel.FillMode.Value = PolygonFillMode.Wireframe;
            //GraphicsSystem.AddModels(colliderModel);

            //var shape = daeMeshProvider.CombineVertexPosition32Bit().GetPhysicsMesh(Simulation.BufferPool);
            //var colliderBehavoir = new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(Simulation, models, shape: shape);
            //models.First().AddBehavoirs(colliderBehavoir);
            CurrentScene.AddCullRenderables(models);
        }

        //private void LoadPhysicObjects()
        //{
        //    for (var j = -10; j < 10; j++)
        //    {
        //        for (var i = -5; i < 5; i++)
        //        {
        //            var m = SphereModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem,
        //                shaders, texture: emptyTexture, sectorCount: 25, stackCount: 25, red: 1, blue: 1, green: 1,
        //                creationInfo: new ModelCreationInfo { Position = new Vector3(i * 5, i + 5 + 15 + j + 5 + 50 * 3, j * 5) },
        //                name: $"physicsSphere{j}:{i}").AddBehavoirs(x => new BepuPhysicsCollidableBehavoir<Sphere>(Simulation, x, bodyType: BepuPhysicsBodyType.Dynamic));
        //            m.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;
        //            CurrentScene.AddCullRenderables(m);
        //        }
        //    }
        //}

        private async Task LoadLargeModelsAsync()
        {
            // currently the obj file doens doesn't support mtlib file names with spaces and the mtl file does not support map_Ks values (released version)
            var modelLoaders = new List<Task<MeshRenderer[]>>();
            modelLoaders.Add(AssimpDaeModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene 3.dae", new ModelLoadOptions {  Transform = new Transform { Position = new Vector3(1000, 100, 0) } }/*, ssvv*/));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene.obj", new ModelLoadOptions {  Transform = new Transform { Position = new Vector3(-1000, 100, 0) } }));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/sponza.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(0, 100, -1000), Scale = Vector3.One * 0.1f } }));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene dark.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(0, 100, 1000) } }));

            var completedModels = await Task.WhenAll(modelLoaders);
            CurrentScene.AddCullRenderables(completedModels.SelectMany(x => x).ToArray());
        }

        //private async Task LoadCarsAsync(TextureView texture)
        //{
        //    var carModels = new List<(MeshDataProvider<VertexPositionColorNormalTexture, Index32>[] Model, Vector3 Forward, Vector3 Up)>();
            
        //    //TODO: make those work
        //    carModels.Add((Model: await DaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae"), Forward: new Vector3(1, 0, 0), Up: new Vector3(0, 0, 1)));
        //    //carModels.Add((Model: await AssimpDaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae"), Forward: new Vector3(1, 0, 0), Up: new Vector3(0, 0, 1)));

        //    //TODO: use same mesh data and buffer just different difuse texture (cords)
        //    var objMesh = await ObjModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car_base.obj");
        //    //var objMesh = await AssimpDaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae");
        //    //var newColors = new[] { RgbaFloat.Red, RgbaFloat.Green, RgbaFloat.Yellow, RgbaFloat.Orange, RgbaFloat.Pink, RgbaFloat.Blue, RgbaFloat.Green, RgbaFloat.White, RgbaFloat.DarkRed, RgbaFloat.CornflowerBlue };
        //    //carModels.AddRange(CreateColoredCarMeshes(objMesh, newColors).Select(providers => (Model: providers, Forward: new Vector3(-1, 0, 0), Up: Vector3.UnitY)).ToArray());

        //    for (var i = 0; i < 20; i++)
        //    {
        //        var meshIndex = i;
        //        while (meshIndex >= carModels.Count)
        //        {
        //            meshIndex -= carModels.Count;
        //        }
        //        var mesh = carModels[meshIndex];
        //        var creationInfo = new ModelCreationInfo { Position = new Vector3(20 * i, -985, 10 * i) };

        //        // TODO support different up then desired up
        //        var carConfig = new CarConfiguration
        //        {
        //            DriveType = (CarDriveType)Standard.Random.GetRandomNumber(0, 3),
        //            ForwardSpeed = Standard.Random.GetRandomNumber(160, 200),
        //            BackwardSpeed = Standard.Random.GetRandomNumber(70, 90),
        //            Mass = Standard.Random.GetRandomNumber(80, 120),
        //            Forward = mesh.Forward,
        //            DesiredForward = -Vector3.UnitX,
        //            Up = mesh.Up,
        //            DesiredUp = GraphicsSystem.Camera.Value!.Up,
        //            CenterOffset = mesh.Forward * 2.7f + mesh.Up * -0.75f,
        //            AngluarIncrease = 1.25f,
        //            AngluarMax = .95f,
        //            SuspensionDirection = -GraphicsSystem.Camera.Value!.Up.Value,
        //            SuspensionLength = 0.01f,
        //            CarLength = 3.3f,
        //            CarWidth = 2.1f,
        //            WheelLength = 0.8f,
        //            WheelMass = 3f,
        //            WheelRadius = 0.9f
        //        };

        //        cars.Add(Car.Create(CurrentScene, GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, collidableProperties, carConfig, mesh.Model, shaders, texture, creationInfo, deviceBufferPool, commandListPool));
        //    }
        //}

        private MeshDataProvider<VertexPositionNormalTextureColor, Index32>[][] CreateColoredCarMeshes(MeshDataProvider<VertexPositionNormalTextureColor, Index32>[] providers, RgbaFloat[] newColors)
            => newColors.Select(newColor => providers.Select(provider => provider.Vertices.First().Color == RgbaFloat.Black ? ChangeCarColor(provider, newColor) : provider).ToArray()).ToArray();


        private MeshDataProvider<VertexPositionNormalTextureColor, Index32> ChangeCarColor(MeshDataProvider<VertexPositionNormalTextureColor, Index32> provider, RgbaFloat newColor)
        {
            return new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(
                provider.Vertices.Select(vertex => new VertexPositionNormalTextureColor(vertex.Position, newColor, vertex.TextureCoordinate, vertex.Normal)).ToArray(), provider.Indices,
                provider.PrimitiveTopology, provider.MaterialName, provider.TexturePath, provider.AlphaMapPath, provider.Material);
        }

        private async Task LoadFloorAsync(TextureView texture)
        {
            var modelMesh = await ObjModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\terrain001.obj");
            var triangles = modelMesh.Meshes.FlattenMeshDataProviders(modelMesh.Instaces).SelectMany(x => x.GetTriangles()).ToArray();
            var meshProvider = new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(
                triangles.SelectMany(x => new[] { x.A, x.B, x.C }).Select(x => new VertexPositionNormalTextureColor(x, RgbaFloat.Red)).ToArray(), Enumerable.Range(0, triangles.Length * 3).Select(x => (Index32)x).ToArray(), PrimitiveTopology.TriangleList);
            var colliderModel = MeshRenderer.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, meshProvider, textureView: texture);
            colliderModel.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;
            CurrentScene.AddCullRenderables(colliderModel);

            var floorModel = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\terrain001.obj", new ModelLoadOptions { Transform = new Transform { Position = GraphicsSystem.Camera.Value.Up.Value * -8f }, PhysicsBufferPool = BepuSimulation?.BufferPool });
            floorModel[0].MeshBuffer.TextureView.Value = texture;
            CurrentScene.AddUpdateables(floorModel.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
            CurrentScene.AddCullRenderables(floorModel);
        }

        private async Task LoadRacingTracksAsync()
        {
            var track = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\track001.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(10, 0, -25) }, PhysicsBufferPool = BepuSimulation?.BufferPool });
            CurrentScene.AddUpdateables(track.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
            CurrentScene.AddCullRenderables(track);

            var track2 = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\track002.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(10, -25, -25) }, PhysicsBufferPool = BepuSimulation?.BufferPool });
            CurrentScene.AddUpdateables(track2.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
            CurrentScene.AddCullRenderables(track2);
        }

        private void LoadParticleSystem()
        {
        //    var computeParticleShader = ResourceFactory.CreateFromSpirv(new ShaderDescription(
        //        ShaderStages.Compute,
        //        File.ReadAllBytes(@"resources\shaders\particle_compute.glsl"),
        //        entryPoint: "main", ApplicationContext.IsDebug));
        //    var shaders = ResourceFactory.CreateFromSpirv(
        //        new ShaderDescription(
        //            ShaderStages.Vertex,
        //            File.ReadAllBytes(@"resources\shaders\particle_vertex.glsl"),
        //            "main"),
        //        new ShaderDescription(
        //            ShaderStages.Fragment,
        //            File.ReadAllBytes(@"resources\shaders\particle_fragment.glsl"),
        //            "main"));
        //    var initialParticles = new ParticleInfo[ParticleSystem.MaxParticles];
        //    for (int i = 0; i < ParticleSystem.MaxParticles; i++)
        //    {
        //        ParticleInfo pi = new ParticleInfo(
        //            new Vector3(40, 20, 40),
        //            new Vector3(Standard.Random.GetRandomNumber(5f, 50f), Standard.Random.GetRandomNumber(5f, 50f), Standard.Random.GetRandomNumber(5f, 50f)),
        //            new Vector3(Standard.Random.GetRandomNumber(0.1f, 1f), Standard.Random.GetRandomNumber(0.1f, 1f), Standard.Random.GetRandomNumber(0.1f, 1f)),
        //            new Vector4(Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f)));
        //        initialParticles[i] = pi;
        //    }
        //    particleSystem = new ParticleSystem(GraphicsDevice, ResourceFactory, GraphicsSystem, computeParticleShader, shaders, initialParticles);
        }
        private void PlayLoadingAudio()
        {
            _ = Task.Run(async () =>
            {
                var loadingContext = AudioSystem.PlayWav(dashRunner);

                while (loadingContext.Volume >= 10)
                {
                    loadingContext.Volume -= (int)(loadingContext.Volume / 5f);
                    await Task.Delay(300);
                }

                AudioSystem.PlayWav(dashRunner, volume: 55);
                AudioSystem.PlayWav(dashRunner, volume: 25);
            });
        }
    }

    public class Wheel
    {
        public Vector3 SuspensionDirection { get; init; }
        public AngularHinge Hinge { get; init; }
        public ConstraintHandle HindgeHandle { get; init; }
        public ConstraintHandle MotorHandle { get; init; }
        public ConstraintHandle LinearAxisServoHandle { get; init; }
        public ConstraintHandle PointOnLineServoHandle { get; init; }
        public BepuPhysicsCollidableBehavoir<Cylinder> CollidableBehavoir { get; init; }
        public MeshRenderer Model { get; init; }

        public Wheel(Vector3 suspensionDirection, AngularHinge hinge, ConstraintHandle hindgeHandle, ConstraintHandle motorHandle, ConstraintHandle linearAxisServoHandle, ConstraintHandle pointOnLineServoHandle, BepuPhysicsCollidableBehavoir<Cylinder> collidableBehavoir, MeshRenderer model)
        {
            SuspensionDirection = suspensionDirection;
            Hinge = hinge;
            HindgeHandle = hindgeHandle;
            MotorHandle = motorHandle;
            LinearAxisServoHandle = linearAxisServoHandle;
            PointOnLineServoHandle = pointOnLineServoHandle;
            CollidableBehavoir = collidableBehavoir;
            Model = model;
        }
    }
    public enum CarDriveType
    {
        Forward,
        Backward,
        All
    }
    public class CarConfiguration
    {
        public CarDriveType DriveType { get; init; } = CarDriveType.All;
        public float ForwardSpeed { get; init; } = 180f;
        public float BackwardSpeed { get; init; } = 80f;
        public float Mass { get; init; } = 100f;
        public float CarWidth { get; init; } = 2.1f;
        public float CarLength { get; init; } = 3.5f;
        public Vector3 CenterOffset { get; init; } = Vector3.UnitZ * 2.6f;
        public float AngluarIncrease { get; init; } = 1.25f;
        public float AngluarMax { get; init; } = 1f;
        public float WheelRadius { get; init; } = 1.2f;
        public float WheelLength { get; init; } = 0.8f;
        public float WheelMass { get; init; } = 3f;
        public float SuspensionLength { get; init; } = 0;
        public Vector3 SuspensionDirection { get; init; } = -Vector3.UnitY;
        public Vector3 Up { get; init; } = Vector3.UnitY;
        public Vector3 Forward { get; init; } = Vector3.UnitZ;

        public Vector3 DesiredUp { get; init; } = Vector3.UnitY;
        public Vector3 DesiredForward { get; init; } = Vector3.UnitY;
    }
    public class Car : IDisposable
    {
        public readonly Transform Transform;
        public readonly IReadOnlyCollection<MeshRenderer> Models;
        public readonly CarConfiguration Configuration;

        private readonly Simulation simulation;
        private readonly Font font = SystemFonts.Families.First().CreateFont(50f);
        private readonly TextModelBuffer textModelBuffer;
        private readonly BepuPhysicsCollidableBehavoir<BepuPhysicsMesh> behavoir;
        private readonly Wheel[] wheels;

        private float previousSpeed = 0f;
        private float previousSteeringAngle = 0f;

        private Car(Simulation simulation, MeshRenderer[] models, BepuPhysicsCollidableBehavoir<BepuPhysicsMesh> behavoir, Wheel[] wheels, CarConfiguration carConfiguration, Transform transform, TextModelBuffer textModelBuffer)
        {
            this.Models = models;
            this.Transform = transform;
            this.Configuration = carConfiguration;

            this.textModelBuffer = textModelBuffer;
            this.behavoir = behavoir;
            this.wheels = wheels;
            this.simulation = simulation;
        }

        private static Wheel CreateWheel(
            Scene scene, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, 
            CollidableProperty<SubgroupCollisionFilter> collidableProperty, CarConfiguration carConfiguration, 
            in BodyHandle bodyHandle, in RigidPose bodyPose, in AngularHinge hindge, in Vector3 bodyToWheelSuspension, in Vector3 left,
            TextureView wheelTexture, DeviceBufferPool? deviceBufferPool = null)
        {
            var wheelShape = new Cylinder(carConfiguration.WheelRadius, carConfiguration.WheelLength);
            var inertia = wheelShape.ComputeInertia(carConfiguration.WheelMass);

            RigidPose wheelPose;
            // do we need those transforms ? (it is constrained by the springs)
            RigidPose.Transform(bodyToWheelSuspension + carConfiguration.SuspensionDirection * carConfiguration.SuspensionLength, bodyPose, out wheelPose.Position);
            QuaternionEx.ConcatenateWithoutOverlap(QuaternionEx.CreateFromRotationMatrix(
                Matrix.CreateFromAxisAngle(left, MathHelper.ToDegrees(90)) * 
                Matrix.CreateFromAxisAngle(carConfiguration.DesiredUp, MathHelper.ToDegrees(90))), 
                bodyPose.Orientation, out wheelPose.Orientation);

            var transform = new Transform { Position = wheelPose.Position, Rotation = Matrix4x4.CreateFromQuaternion(wheelPose.Orientation), Scale = new Vector3(carConfiguration.WheelRadius * 2, carConfiguration.WheelLength, carConfiguration.WheelRadius * 2) };
            var model = SphereModel.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: transform, sectorCount: 12, stackCount: 12, texture: wheelTexture, deviceBufferPool: deviceBufferPool, radius: carConfiguration.WheelRadius / 2f);
            model.Name = "wheel";
            model.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;

            var behavoir = new BepuPhysicsCollidableBehavoir<Cylinder>(simulation, model, inertia, BepuPhysicsBodyType.Dynamic, shape: wheelShape);
            var motor = simulation.Solver.Add(behavoir.Collider.BodyHandle!.Value, bodyHandle, new AngularAxisMotor
            {
                LocalAxisA = carConfiguration.DesiredUp,
                Settings = default,
                TargetVelocity = default
            });
            var linearAxisServo = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, new LinearAxisServo
            {
                LocalPlaneNormal = carConfiguration.SuspensionDirection,
                TargetOffset = carConfiguration.SuspensionLength,
                LocalOffsetA = bodyToWheelSuspension,
                LocalOffsetB = default,
                ServoSettings = ServoSettings.Default,
                SpringSettings = new SpringSettings(5f, 0.4f)
            });
            var pointOnLineServo = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, new PointOnLineServo
            {
                LocalDirection = carConfiguration.SuspensionDirection,
                LocalOffsetA = bodyToWheelSuspension,
                LocalOffsetB = default,
                ServoSettings = ServoSettings.Default,
                SpringSettings = new SpringSettings(30, 0.5f)
            });
            var hindgeHandle = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, hindge);

            ref var wheelProperties = ref collidableProperty.Allocate(behavoir.Collider.BodyHandle!.Value);
            wheelProperties = new SubgroupCollisionFilter { GroupId = bodyHandle.Value };

            scene.AddUpdateables(behavoir);
            scene.AddCullRenderables(model);
            return new Wheel(carConfiguration.SuspensionDirection, hindge, hindgeHandle, motor, linearAxisServo, pointOnLineServo, behavoir, model);
        }

        public static Car Create(
            Scene scene, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, 
            CollidableProperty<SubgroupCollisionFilter> collidableProperty, CarConfiguration carConfiguration, 
            BaseMeshDataProvider[] meshDataProviders, TextureView texture, Transform transform, 
            DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        {
            var left = Vector3.Normalize(Vector3.One - Vector3.Abs(carConfiguration.Up) - Vector3.Abs(carConfiguration.Forward) * carConfiguration.Forward.Length());

            // TODO: apply transform before creating model
            var angle = (float)Math.Acos(Vector3.Dot(carConfiguration.DesiredForward, carConfiguration.Forward) / (carConfiguration.DesiredForward.Length() * carConfiguration.Forward.Length()));
            var realTransform = transform with { Rotation = transform.Rotation * Matrix4x4.CreateFromAxisAngle(carConfiguration.Up, angle) };

            var buffers = meshDataProviders.Select(meshData => MeshDeviceBuffer.Create(graphicsDevice, resourceFactory, meshData, textureView: texture, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool)).ToArray();
            var models = buffers.Select(buffer => new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, buffer, transform: realTransform, name: "car")).ToArray();
            scene.AddCullRenderables(models);

            var shape = meshDataProviders.CombineVertexPosition32Bit().GetPhysicsMesh(simulation.BufferPool);
            var colliderBehavoir = new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(simulation, models, carConfiguration.Mass, BepuPhysicsBodyType.Dynamic, shape: shape);
            var body = simulation.Bodies[colliderBehavoir.Collider.BodyHandle ?? throw new Exception()];
            ref var bodyProperties = ref collidableProperty.Allocate(body.Handle);
            bodyProperties = new SubgroupCollisionFilter { GroupId = body.Handle.Value };
            scene.AddUpdateables(colliderBehavoir);


            // TODO: do depending on up
            QuaternionEx.TransformUnitY(QuaternionEx.CreateFromAxisAngle(carConfiguration.Forward, MathF.PI * 0.5f), out var wheelAxis);
            var hindge = new AngularHinge
            {
                LocalHingeAxisA = wheelAxis,
                LocalHingeAxisB = carConfiguration.Up,
                SpringSettings = new SpringSettings(30, 1)
            };
            var handles1 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, carConfiguration.Forward * carConfiguration.CarLength + left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
            var handles2 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, carConfiguration.Forward * carConfiguration.CarLength - left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
            var handles3 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, -carConfiguration.Forward * carConfiguration.CarLength + left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
            var handles4 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, -carConfiguration.Forward * carConfiguration.CarLength - left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);

            var createTextBufferBehavoir = new Action<MeshRenderer>(meshRenderer => scene.AddUpdateables(new AlwaysFaceCameraBehavior(meshRenderer, Matrix4x4.CreateRotationX(MathHelper.ToRadians(90)))));
            var textBuffer = new TextModelBuffer(scene, graphicsDevice, resourceFactory, graphicsSystem, deviceBufferPool, commandListPool, createTextBufferBehavoir);
            textBuffer.SetTransform(scale: Vector3.One * 0.05f);

            return new Car(simulation, models, colliderBehavoir, new[] { handles1, handles2, handles3, handles4 }, carConfiguration, realTransform, textBuffer);
        }

        public void Reset()
        {
            var body = behavoir.Collider.GetBodyReference();
            body.Velocity = Vector3.Zero;
            foreach (var model in Models)
            {
                model.Transform.Value = Transform;
            }
            foreach (var wheel in wheels)
            {
                wheel.Model.Transform.Value = wheel.Model.Transform.Value with { Position = Transform.Position };
            }
        }

        public void Update(InputHandler inputHandler, float delta)
        {
            var speed = 0f;
            var angle = 0f;
            if (inputHandler.IsKeyDown(Key.Up))
            {
                speed = Configuration.ForwardSpeed;
            }
            if (inputHandler.IsKeyDown(Key.Down))
            {
                speed = -Configuration.BackwardSpeed;
            }

            if (inputHandler.IsKeyDown(Key.Left))
            {
                angle = Math.Min(previousSteeringAngle + Configuration.AngluarIncrease * delta, Configuration.AngluarMax);
            }
            else if (inputHandler.IsKeyDown(Key.Right))
            {
                angle = Math.Max(previousSteeringAngle - Configuration.AngluarIncrease * delta, -Configuration.AngluarMax);
            }
            else
            {
                angle = previousSteeringAngle >= Configuration.AngluarIncrease * delta
                    ? previousSteeringAngle - Configuration.AngluarIncrease * delta
                    : previousSteeringAngle <= -Configuration.AngluarIncrease * delta
                    ? previousSteeringAngle + Configuration.AngluarIncrease * delta
                    : 0;
            }

            if (previousSpeed != speed)
            {
                var motoredWheels = Configuration.DriveType == CarDriveType.All ? wheels :
                                    Configuration.DriveType == CarDriveType.Forward ? wheels.Take(2) :
                                    wheels.Skip(2);

                foreach (var wheel in motoredWheels)
                {
                    simulation.Solver.ApplyDescription(wheel.MotorHandle, new AngularAxisMotor
                    {
                        LocalAxisA = -Configuration.DesiredUp,
                        Settings = new MotorSettings(100, 1e-6f),
                        TargetVelocity = speed
                    });
                }
                previousSpeed = speed;
            }

            if (previousSteeringAngle != angle)
            {
                foreach (var wheel in wheels.Take(2))
                {
                    var steeredHinge = wheel.Hinge;
                    Matrix3x3.CreateFromAxisAngle(Configuration.SuspensionDirection, -angle, out var rotation);
                    Matrix3x3.Transform(wheel.Hinge.LocalHingeAxisA, rotation, out steeredHinge.LocalHingeAxisA);
                    simulation.Solver.ApplyDescription(wheel.HindgeHandle, steeredHinge);
                }
                previousSteeringAngle = angle;
            }

            var mainModel = Models.First();
            var body = behavoir.Collider.GetBodyReference();

            // TODO update only if is cullable?
            var speedText = Math.Round(body.Velocity.Linear.Length(), 2).ToString("0.00");
            var text = $"{speedText} m/s";
            textModelBuffer.Write(font, text, RgbaFloat.Black);
            textModelBuffer.SetTransform(position: mainModel.Transform.Value.Position + Configuration.DesiredUp * 5f);
        }

        public void Dispose()
        {
            foreach(var model in this.Models)
            {
                model.Dispose();
            }
        }
    }
    class SampleContactEventHandler : IContactEventHandler
    {
        private const string contactSound = @"resources/audio/contact.wav";

        private readonly Game game;
        private readonly IAudioSystem audioSystem;
        private readonly ILogger<SampleContactEventHandler> logger = ApplicationContext.LoggerFactory.CreateLogger<SampleContactEventHandler>();

        public SampleContactEventHandler(Game game, IAudioSystem audioSystem)
        {
            this.game = game;
            this.audioSystem = audioSystem;
            this.audioSystem.PreLoadWav(contactSound);
        }

        public void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        {
            const int maxVolume = 60;
            const float maxIntensity = 16;

            if (manifold.Count <= 0)
                return;

            manifold.GetContact(0, out var offset, out _, out var depth, out _);
            var volume = maxVolume / 2f * Math.Min(2f, Math.Abs(depth));

            if (volume > 3f)
            {
                var dynamicBody = pair.A.Mobility == CollidableMobility.Dynamic ? pair.A : pair.B;
                var body = game.BepuSimulation.Bodies.GetBodyReference(dynamicBody.BodyHandle);
                var position = offset + body.Pose.Position;
                logger.LogInformation($"A colision with a deth of {depth} occured which will create a sound with a volume of {volume} at {position}");
                audioSystem.PlaceWav(contactSound, position, maxIntensity / maxVolume * volume, (int)volume);
            }
        }
    }
}

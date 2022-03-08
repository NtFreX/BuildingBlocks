using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Common;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
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
using ProtoBuf;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
//using imnodesNET;

using BepuPhysicsMesh = BepuPhysics.Collidables.Mesh;

//TODO: material shader for textures to achieve things link simple materials or even fire! https://cyangamedev.wordpress.com/2020/08/04/fire-shader-breakdown/
//TODO: print number of vertices drawn (+more vertice stats)
//TODO: print command pool and device buffer pool stats
namespace NtFreX.BuildingBlocks.Sample
{
    //TODO: multithreading and  tasks!!!!!!!!!!!!!!

    //public class GraphicsNodeEditor
    //{
    //    public static void AfterGraphicsSystemUpdate(float delta)
    //    {
    //        ImGui.Begin("Graphics system");
    //        //imnodes.BeginNodeEditor();

    //        //imnodes.BeginNode(100);
    //        //imnodes.BeginNodeTitleBar();
    //        //ImGui.TextUnformatted("Shadow pass");
    //        //imnodes.EndNodeTitleBar();

    //        //imnodes.BeginInputAttribute(001);
    //        //ImGui.Text("NearLimit");
    //        //imnodes.EndInputAttribute();

    //        //imnodes.BeginInputAttribute(002);
    //        //ImGui.Text("FarLimit");
    //        //imnodes.EndInputAttribute();

    //        //imnodes.BeginInputAttribute(002);
    //        //ImGui.Text("FarLimit");
    //        //imnodes.EndInputAttribute();

    //        //imnodes.BeginInputAttribute(002);
    //        //ImGui.Text("Resolution");
    //        //imnodes.EndInputAttribute();

    //        //imnodes.BeginOutputAttribute(001);
    //        //ImGui.Text("Texture");
    //        //imnodes.EndOutputAttribute();
    //        //imnodes.EndNode();

    //        //imnodes.EndNodeEditor();
    //        ImGui.End();
    //    }
    //}
    public class CenterQubeComponent
    {
        private readonly PhongMaterialMeshDataSpecialization materialMeshDataSpecialization;

        public CenterQubeComponent(PhongMaterialMeshDataSpecialization materialMeshDataSpecialization)
        {
            this.materialMeshDataSpecialization = materialMeshDataSpecialization;
        }

        public static async Task<CenterQubeComponent> CreateAsync(Simulation? simulation, Scene scene, TextureProvider blueTextureProvider)
        {
            //TODO: use instanced drawing or at least reuse mesh buffer
            var qubeSideLength = .5f;
            var material = new PhongMaterialMeshDataSpecialization(new PhongMaterialInfo { Shininess = .2f, ShininessStrength = .2f, DiffuseColor = new Vector4(.2f, .2f, .2f, 1f), SpecularColor = new Vector4(.2f, .2f, .2f, 1f), AmbientColor = new Vector4(.2f, .2f, .2f, 1f) } ); //TODO: does it need a material if it has vertex colors?
            var centerQubes = new MeshRenderer[] {
                //await QubeMesh.CreateAsync(transform: new Transform { Position = Vector3.Zero }, sideLength: qubeSideLength, specializations: new [] { material }),

                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, -4) }, sideLength: qubeSideLength, specializations: new MeshDataSpecialization[] { material, new SurfaceTextureMeshDataSpecialization(blueTextureProvider) }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, -3) }, sideLength: qubeSideLength, blue: .5f, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, -2) }, sideLength: qubeSideLength, green: .5f, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, -1) }, sideLength: qubeSideLength, red: .5f, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, 1) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, 2) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, 3) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 0, 4) }, sideLength: qubeSideLength, specializations: new [] { material }),

                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, -4, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, -3, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, -2, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, -1, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 1, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 2, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 3, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(0, 4, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),

                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-4, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-3, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-2, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-1, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(1, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(2, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(3, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(4, 0, 0) }, sideLength: qubeSideLength, specializations: new [] { material }),

                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-4, -4, -4) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-3, -3, -3) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-2, -2, -2) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(-1, -1, -1) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(1, 1, 1) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(2, 2, 2) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(3, 3, 3) }, sideLength: qubeSideLength, specializations: new [] { material }),
                await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(4, 4, 4) }, sideLength: qubeSideLength, specializations: new [] { material }),
            };
            if (simulation != null)
            {
                scene.AddUpdateables(centerQubes.Select(x => new BepuPhysicsCollidableBehavoir<Box>(simulation, x)).ToArray());
                // scene.AddUpdateables(centerQubes); this is only nessesary if the meshrenderer has animations or it's transform changes
            }
            await scene.AddCullRenderablesAsync(centerQubes);
            return new CenterQubeComponent(material);
        }

        public void SetOpacity(float value)
        {
            materialMeshDataSpecialization.Material.Value = materialMeshDataSpecialization.Material.Value with { Opacity = value };
        }
    }

    public class SunComponent
    {
        private readonly LightSystem lightSystem;
        private readonly MeshRenderer sun;

        public Vector3 Position => sun.Transform.Value.Position;

        public const float SunDistance = 50f;

        private float currentDegrees = 0f;

        private SunComponent(LightSystem lightSystem, MeshRenderer sun)
        {
            this.lightSystem = lightSystem;
            this.sun = sun;
        }

        public static async Task<SunComponent> CreateAsync(Scene scene)
        {
            Debug.Assert(scene.LightSystem.Value != null);

            //TODO: exclude  sphere from shadow renderer
            var sun = await SphereMesh.CreateAsync(transform: new Transform { Position = Vector3.Zero }, red: 1f, green: 1, alpha: 1f, radius: 5f, sectorCount: 25, stackCount: 25, name: "sun");
            await scene.AddFreeRenderablesAsync(sun);
            scene.AddUpdateables(sun);
            return new SunComponent(scene.LightSystem.Value, sun);
        }

        public void Update(float delta)
        {
            var sunSpeed = sun.Transform.Value.Position.Y < 0 ? 10f : 5f;

            currentDegrees += sunSpeed * delta;
            currentDegrees = currentDegrees > 360 ? 0f : currentDegrees;

            var rotation = Matrix4x4.CreateRotationX(MathHelper.ToRadians(currentDegrees), Vector3.Zero);
            var lightPos = Vector3.Transform(Vector3.UnitY, rotation) * SunDistance;

            var ambient = Math.Max(lightPos.Y / SunDistance, 0) * .2f + 0.005f;
            lightSystem.AmbientLight = new Vector4(ambient, ambient, ambient, 1f);

            var directional = Math.Max(lightPos.Y / SunDistance, 0) * .8f;
            lightSystem.DirectionalLightDirection = Vector3.Normalize(-sun.Transform.Value.Position);
            lightSystem.DirectionalLightColor = new Vector4(directional, directional, directional / 6f, 1f);

            sun.Transform.Value = sun.Transform.Value with { Position = lightPos };
        }
    }

    public class SampleGame : Game
    {
        private CenterQubeComponent? centerQubeComponent;
        private SunComponent sunComponent;
        private ParticleRenderer centerParticleRenderer;

        private const string dashRunner = @"resources/audio/Dash Runner.wav";
        private const string detective = @"resources/audio/8-bit Detective.wav";

        //private MeshRenderer rotatingQube;
        //private MeshRenderer[]? goblin;
        //private MeshRenderer[]? dragon;

        //private TextureView? emptyTexture;
        //private ParticleSystem particleSystem;

        private readonly Font[] font; //= SystemFonts.Families.Select(x => x.CreateFont(48, FontStyle.Regular)).ToArray();

        //private long elapsedMilisecondsSincePhyicsObjectAdd = 0;
        //private Stopwatch stopwatch = Stopwatch.StartNew();
        //private long elapsedMilisecondsSinceParticleReset = 0;

        // TODO create and destroy device resource pattern
        private DeviceBufferPool deviceBufferPool = new DeviceBufferPool(128);
        //private CommandListPool commandListPool; // use commandlist pool of game?
        private TextMesh? elapsedTextMesh;

        private MovableCamera? movableCamera;
        //private ThirdPersonCamera thirdPersonCamera;

        private IntPtr? dublicatorViewPtr;
        private IntPtr? shadowmapNear;
        private IntPtr? shadowmapMid;
        private IntPtr? shadowmapFar;

        //private List<Model> physicsModels = new List<Model>();
        //private List<Car> cars = new List<Car>();

        //private readonly CollidableProperty<SubgroupCollisionFilter> collidableProperties = new CollidableProperty<SubgroupCollisionFilter>();

        public SampleGame()
        {
            var fontCollection = new FontCollection();
            fontCollection.Install("resources/fonts/ubuntu.regular.ttf");
            font = new[] { fontCollection.CreateFont("Ubuntu", 48, FontStyle.Regular) };
        }

        protected override async Task SetupAsync(IShell shell, ILoggerFactory loggerFactory)
        {
            FrameLimitter = new FrameLimitter(144); //TODO: disable from time to time to make sure perf is top notch
            EnableImGui = true;
            AudioSystemType = AudioSystemType.Sdl2;
            //EnableBepuSimulation = true;

            movableCamera = new MovableCamera(shell.Width, shell.Height);

            var scene = new Scene(shell.IsDebug);
            scene.LightSystem.Value = new LightSystem();
            scene.Camera.Value = movableCamera;
            await ChangeSceneAsync(scene);

            await base.SetupAsync(shell, loggerFactory);
        }

        //protected override Simulation LoadSimulation() => Simulation.Create(new BepuUtilities.Memory.BufferPool(), new SubgroupFilteredCallbacks(new SampleContactEventHandler(this, AudioSystem), collidableProperties), new NullPoseIntegratorCallbacks(), new SolveDescription(1, 4));

        protected override async Task BeforeGraphicsSystemUpdateAsync(float delta)
        {
            Debug.Assert(elapsedTextMesh != null);
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


            var text = Stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " total seconds elapsed";
            await elapsedTextMesh.WriteAsync(font[0], text, new RgbaFloat(0, 1, 0, 0));
            //await elapsedTextMesh.WriteAsync(text.Select((item, index) => new TextData(font[index], text[index].ToString(), new RgbaFloat(Standard.Random.GetRandomNumber(0f, 1f), Standard.Random.GetRandomNumber(0f, 1f), Standard.Random.GetRandomNumber(0f, 1f), 1))).ToArray());
            //elapsedTextMesh.SetTransform(position: new Vector3(-20, 0, -20), scale: new Vector3(0.001f));

        }
        protected override async Task AfterGraphicsSystemUpdateAsync(float delta)
        {
            await base.AfterGraphicsSystemUpdateAsync(delta);


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


            sunComponent.Update(delta);
            centerQubeComponent?.SetOpacity(sunComponent.Position.Y / SunComponent.SunDistance);


            //if(Stopwatch.ElapsedMilliseconds - 1000 > elapsedMilisecondsSinceParticleReset)
            //{
            //    centerParticleRenderer.SetParticles(GetParticles());
            //    elapsedMilisecondsSinceParticleReset = Stopwatch.ElapsedMilliseconds;
            //}


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
            //GraphicsNodeEditor.AfterGraphicsSystemUpdate(delta);

            ImGui.Begin("Views");
            if (dublicatorViewPtr != null)
                ImGui.Image(dublicatorViewPtr.Value, new Vector2(150, 150));
            if (shadowmapNear != null)
                ImGui.Image(shadowmapNear.Value, new Vector2(150, 150));
            if (shadowmapMid != null)
                ImGui.Image(shadowmapMid.Value, new Vector2(150, 150));
            if (shadowmapFar != null)
                ImGui.Image(shadowmapFar.Value, new Vector2(150, 150));
            ImGui.End();

            if (CurrentScene?.LightSystem.Value != null)
            {
                Vector4 ambientLight = CurrentScene.LightSystem.Value.AmbientLight;
                Vector4 directionalLight = CurrentScene.LightSystem.Value.DirectionalLightColor;
                Vector3 directionalLightDirection = CurrentScene.LightSystem.Value.DirectionalLightDirection;

                ImGui.Begin("Light");
                if (ImGui.ColorPicker4("Ambient light color", ref ambientLight))
                {
                    CurrentScene.LightSystem.Value.AmbientLight = ambientLight;
                    InputHandler.HandleMouseEvents(MouseButton.Left);
                }
                if (ImGui.ColorPicker4("Directional light color", ref directionalLight))
                {
                    CurrentScene.LightSystem.Value.DirectionalLightColor = directionalLight;
                    InputHandler.HandleMouseEvents(MouseButton.Left);
                }
                if (ImGui.SliderFloat3("Directional light direction", ref directionalLightDirection, v_min: -1f, v_max: 1f))
                {
                    CurrentScene.LightSystem.Value.DirectionalLightDirection = directionalLightDirection;
                    InputHandler.HandleMouseEvents(MouseButton.Left);
                }
                ImGui.End();
            }

            if (CurrentScene != null)
            {
                ImGui.Begin("Items");
                foreach (var cull in CurrentScene.CullRenderables)
                {
                    if (ImGui.Button(cull.GetType().Name + ": " + cull.GetCenter()) && movableCamera != null)
                    {
                        movableCamera.Position.Value = movableCamera.GetPositionFromLookAt(cull.GetCenter(), distance: 5f);
                        movableCamera.SetLookAt();

                        if(cull is MeshRenderer meshRenderer)
                        {
                            meshRenderer.IsActive.Value = false;
                            Task.Delay(500).ContinueWith(t => meshRenderer.IsActive.Value = true);
                        }
                    }
                }
                foreach (var item in CurrentScene.FreeRenderables)
                {
                    ImGui.Text(item.GetType().Name);
                }
                ImGui.End();
            }
        }

        protected override void BeforeRenderContextCreated()
        {
            base.BeforeRenderContextCreated();
            DestroyRenderContextResources();
        }
        protected override void AfterWindowResized()
        {
            base.AfterWindowResized();
            ResizeWindowSizedResources();
        }
        protected override void BeforeGraphicsDeviceDestroyed()
        {
            base.BeforeGraphicsDeviceDestroyed();
            DestroyRenderContextResources();
        }
        protected override async Task AfterGraphicsDeviceCreatedAsync()
        {
            await base.AfterGraphicsDeviceCreatedAsync();

            Debug.Assert(TextureFactory != null);
            Debug.Assert(CurrentScene?.LightSystem.Value != null);
            Debug.Assert(CurrentScene.Camera.Value != null);
            Debug.Assert(RenderContext != null);
            Debug.Assert(Shell != null);

            ResizeWindowSizedResources();

            shadowmapNear = GetOrCreateImGuiBinding(RenderContext.NearShadowMapView);
            shadowmapMid = GetOrCreateImGuiBinding(RenderContext.MidShadowMapView);
            shadowmapFar = GetOrCreateImGuiBinding(RenderContext.FarShadowMapView);
            //commandListPool = new CommandListPool(ResourceFactory);

            //PlayLoadingAudio();

            //emptyTexture = TextureFactory.GetEmptyTexture(TextureUsage.Sampled);
            //var stoneTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/spnza_bricks_a_diff.png", TextureUsage.Sampled);
            //var blueTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/app.png", TextureUsage.Sampled);

            //LoadParticleSystem();

            //TODO: why are they not working
            CurrentScene.LightSystem.Value.SetPointLights(
                new PointLightInfo { Color = new Vector4(.3f, 0, 0, 1f), Intensity = .2f, Position = Vector3.UnitX * 10 - Vector3.UnitY * 4, Range = 2f },
                new PointLightInfo { Color = new Vector4(0, .3f, 0, 1f), Intensity = .2f, Position = Vector3.UnitZ * 10 - Vector3.UnitY * 4, Range = 2f },
                new PointLightInfo { Color = new Vector4(0, 0, .3f, 1f), Intensity = .2f, Position = -Vector3.UnitX * 10 - Vector3.UnitY * 4, Range = 2f },
                new PointLightInfo { Color = new Vector4(.3f, .3f, .3f, 1f), Intensity = .2f, Position = -Vector3.UnitZ * 10 - Vector3.UnitY * 4, Range = 2f },

                new PointLightInfo { Color = new Vector4(.3f, 0, 0, 1f), Intensity = .5f, Position = Vector3.UnitX * 30 - Vector3.UnitY * 4, Range = 4f },
                new PointLightInfo { Color = new Vector4(0, .3f, 0, 1f), Intensity = .5f, Position = Vector3.UnitZ * 30 - Vector3.UnitY * 4, Range = 4f },
                new PointLightInfo { Color = new Vector4(0, 0, .3f, 1f), Intensity = .5f, Position = -Vector3.UnitX * 30 - Vector3.UnitY * 4, Range = 4f },
                new PointLightInfo { Color = new Vector4(.3f, .3f, .3f, 1f), Intensity = .5f, Position = -Vector3.UnitZ * 30 - Vector3.UnitY * 4, Range = 4f },

                new PointLightInfo { Color = new Vector4(.3f, 0, 0, 1f), Intensity = .8f, Position = Vector3.UnitX * 50 - Vector3.UnitY * 4, Range = 8f },
                new PointLightInfo { Color = new Vector4(0, .3f, 0, 1f), Intensity = .8f, Position = Vector3.UnitZ * 50 - Vector3.UnitY * 4, Range = 8f },
                new PointLightInfo { Color = new Vector4(0, 0, .3f, 1f), Intensity = .8f, Position = -Vector3.UnitX * 50 - Vector3.UnitY * 4, Range = 8f },
                new PointLightInfo { Color = new Vector4(.3f, .3f, .3f, 1f), Intensity = .8f, Position = -Vector3.UnitZ * 50 - Vector3.UnitY * 4, Range = 8f }
            );
            await CurrentScene.AddCullRenderablesAsync(
                await SphereMesh.CreateAsync(new Transform(Vector3.UnitX * 10 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(Vector3.UnitZ * 10 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(-Vector3.UnitX * 10 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(-Vector3.UnitZ * 10 - Vector3.UnitY * 4)),

                await QubeMesh.CreateAsync(new Transform(Vector3.UnitX * 30 - Vector3.UnitY * 4)),
                await QubeMesh.CreateAsync(new Transform(Vector3.UnitZ * 30 - Vector3.UnitY * 4)),
                await QubeMesh.CreateAsync(new Transform(-Vector3.UnitX * 30 - Vector3.UnitY * 4)),
                await QubeMesh.CreateAsync(new Transform(-Vector3.UnitZ * 30 - Vector3.UnitY * 4)),

                await SphereMesh.CreateAsync(new Transform(Vector3.UnitX * 50 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(Vector3.UnitZ * 50 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(-Vector3.UnitX * 50 - Vector3.UnitY * 4)),
                await SphereMesh.CreateAsync(new Transform(-Vector3.UnitZ * 50 - Vector3.UnitY * 4)));

            {
                //MeshRenderPassFactory.RenderPasses.Add(new VertexPositionColorNormalTextureMeshRenderPass(ResourceFactory, ApplicationContext.IsDebug));
            }

            var positionRange = new Vector3(200f, 50, 200f);
            centerParticleRenderer = new ParticleRenderer(
                new Transform(CurrentScene.Camera.Value.Up.Value * 5f), 
                GetParticles(positionRange),
                new Veldrid.Utilities.BoundingBox(Vector3.One * -positionRange / 2, Vector3.One * positionRange / 2), 
                new Veldrid.Utilities.BoundingBox(),//new Vector3(-positionRange / 2, -positionRange / 2, -positionRange / 2), new Vector3(positionRange / 2, -positionRange / 2 + 1, positionRange / 2)),
                new DirectoryTextureProvider(TextureFactory, @"resources/models/textures/spnza_bricks_a_diff.png"), Shell.IsDebug);
            await CurrentScene.AddCullRenderablesAsync(centerParticleRenderer);

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
            elapsedTextMesh = new TextMesh(CurrentScene,
                new Transform(/*new Vector3(-20, 0, -20), */scale: Vector3.One / 0.01f), //rotation: Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(90)))),
                deviceBufferPool: deviceBufferPool, commandListPool: CommandListPool);


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

            {
                Debug.Assert(CurrentScene != null);

                //TODO: use dir tex prov
                centerQubeComponent = await CenterQubeComponent.CreateAsync(BepuSimulation, CurrentScene, new DirectoryTextureProvider(TextureFactory, @"resources/models/textures/spnza_bricks_a_diff.png"));
            }

            sunComponent = await SunComponent.CreateAsync(CurrentScene);

            await LoadXYZLineModelsAsync();
            await CreateSkyboxAsync();
            //LoadPhysicObjects();
            await LoadFloorAsync();
            await LoadInstancedAsync();
            //await LoadLargeModelsAsync();
            await CreateAndSaveLoadModelAsync();
            //CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\level_2902.dae")); 
            //CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\wall.dae"));

            //CurrentScene.AddCullRenderables(await AssimpDaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\cart_level002.dae", new ModelLoadOptions {  Transform = new Transform(position: new Vector3(-1000, 0, -1000)) }));
            //CurrentScene.AddCullRenderables(await DaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\cart_level002.dae", new ModelLoadOptions { Transform = new Transform(position: new Vector3(1000, 0, 1000)) }));

            //await CreateAnimatedModelAsync(@"D:\projects\veldrid-samples\assets\models\goblin.dae"); //@"C:\Users\FTR\Documents\Space Station Scene.dae");

            //goblin = await AssimpDaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, -15), Scale = new Vector3(.001f) }, shaders, @"resources/models/goblin.dae");
            //dragon = await AssimpDaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, 15) }, shaders, @"resources/models/chinesedragon.dae");

            //CurrentScene.AddCullRenderables(sun);
            //CurrentScene.AddCullRenderables(goblin);
            //CurrentScene.AddCullRenderables(dragon);

            //await DrawBoundingBoxesAsync();

            //thirdPersonCamera = new ThirdPersonCamera(GraphicsDevice, ResourceFactory, Shell.Width, Shell.Height, new Vector3(0, 0, -1), new Vector3(0, 1, 3));

            //AudioSystem.StopAll();
            //AudioSystem.PlaceWav(detective, loop: true, position: Vector3.Zero, intensity: 100f);

        }

        //protected override void BeforeGraphicsSystemRender(float deleta)
        //{
        //    //particleSystem.Draw(commandList);
        //}

        //private async Task CreateAnimatedModelAsync(string path)
        //{
        //    var goblinModels = await AssimpDaeModelImporter.ModelFromFileAsync(path);
        //    foreach (var model in goblinModels)
        //    {
        //        if (model.MeshBuffer.Material.Value != null)
        //            model.MeshBuffer.Material.Value = model.MeshBuffer.Material.Value.Value with { Opacity = 1f };

        //        // TODO: support cull renderable for animations?
        //        var animation = model.MeshBuffer.BoneAnimationProviders?.FirstOrDefault();
        //        if (animation != null)
        //        {
        //            animation.IsRunning = true;
        //            CurrentScene.AddUpdateables(model);
        //        }
        //        CurrentScene.AddFreeRenderables(model);
        //    }
        //}
        
        private void DestroyRenderContextResources()
        {
            if (dublicatorViewPtr != null)
            {
                Debug.Assert(RenderContext?.DuplicatorTargetView1 != null);
                RemoveImGuiBinding(RenderContext.DuplicatorTargetView1);
            }
            if (shadowmapNear != null)
            {
                Debug.Assert(RenderContext != null);
                RemoveImGuiBinding(RenderContext.NearShadowMapView);
            }
            if (shadowmapMid != null)
            {
                Debug.Assert(RenderContext != null);
                RemoveImGuiBinding(RenderContext.MidShadowMapView);
            }
            if (shadowmapFar != null)
            {
                Debug.Assert(RenderContext != null);
                RemoveImGuiBinding(RenderContext.FarShadowMapView);
            }
        }
        private void ResizeWindowSizedResources()
        {
            if (RenderContext?.DuplicatorTargetView1 != null)
            {
                dublicatorViewPtr = GetOrCreateImGuiBinding(RenderContext.DuplicatorTargetView1);
            }
        }

        private Task CreateSkyboxAsync()
        {
            Debug.Assert(CurrentScene != null);
            Debug.Assert(Shell != null);

            var skybox = new SkyboxRenderer(
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_ft.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_bk.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_lf.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_rt.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_up.png"),
                Image.Load<Rgba32>(@"D:\projects\ntfrex_veldrid\src\NeoDemo\Assets\Textures\cloudtop\cloudtop_dn.png"), Shell.IsDebug);

            // TODO: seperate update and device resource update and remove this!
            CurrentScene.AddUpdateables(skybox);
            return CurrentScene.AddFreeRenderablesAsync(skybox);
        }

        private ParticleRenderer.ParticleInfo[] GetParticles(Vector3 positionRange)
        {
            Debug.Assert(CurrentScene?.Camera.Value != null);

            var initialParticles = new ParticleRenderer.ParticleInfo[1024000];
            for (int i = 0; i < initialParticles.Length; i++)
            {
                var pi = new ParticleRenderer.ParticleInfo(
                    position: new Vector3(Standard.Random.Noise(0, positionRange.X), Standard.Random.Noise(0, positionRange.Y), Standard.Random.Noise(0, positionRange.Z)),
                    scale: (uint)Standard.Random.GetRandomNumber(1f, 5f),
                    velocity: Standard.Random.Noise(-CurrentScene.Camera.Value.Up.Value * .01f, .004f),
                    new Vector4(Standard.Random.GetRandomNumber(.1f, .9f), Standard.Random.GetRandomNumber(.1f, .9f), Standard.Random.GetRandomNumber(.1f, .9f), Standard.Random.GetRandomNumber(.2f, 1f)));
                initialParticles[i] = pi;
            }
            return initialParticles;
        }

        private async Task CreateAndSaveLoadModelAsync()
        {
            Debug.Assert(CurrentScene != null);

            const string fileName = "qube.bin";
            // passing a DynamicMeshDataProvider is not nessesary but it wil in the furture be usefull
            // the goal is to only load mesh data into memory when they are needed. probably this also depends on the garbage collection functionality that sould be impleteded with that
            SpecializedMeshData DynamicProvide()
            {
                var mesh = new DefinedMeshData<VertexPosition, Index16>(
                    new VertexPosition[] { -Vector3.UnitX - Vector3.UnitZ, Vector3.UnitX - Vector3.UnitZ, -Vector3.UnitX + Vector3.UnitZ, Vector3.UnitX + Vector3.UnitZ, Vector3.UnitY },
                    new Index16[] { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 },
                    PrimitiveTopology.TriangleList);

                mesh.WriteTo(fileName);
                return ProtobufSerializableExtensions.ReadFrom<DefinedMeshData<VertexPosition, Index16>.Protobuf, DefinedMeshData<VertexPosition, Index16>>(fileName);
            }
            await CurrentScene.AddCullRenderablesAsync(await MeshRenderer.CreateAsync(new DynamicMeshDataProvider(DynamicProvide), transform: new Transform(new Vector3(0, 100, 0))));
            File.Delete(fileName);
        }

        private async Task DrawBoundingBoxesAsync()
        {
            Debug.Assert(CurrentScene != null);

            foreach (var model in CurrentScene.CullRenderables ?? Array.Empty<CullRenderable>())
            {
                var boundingBox = await BoundingBoxMesh.CreateAsync(model.GetBoundingBox());
                boundingBox.MeshData.DrawConfiguration.FillMode.Value = PolygonFillMode.Wireframe;
                await CurrentScene.AddCullRenderablesAsync(boundingBox);
            }
        }

        private async Task LoadFloorAsync()
        {
            // TODO: add all models as updatables???
            Debug.Assert(CurrentScene?.Camera.Value != null);
            Debug.Assert(TextureFactory != null);

            var plane = await PlaneMesh.CreateAsync(
                    rows: 200, columns: 200, transform: new Transform { Position = CurrentScene.Camera.Value.Up.Value * -5f },
                    name: "floor", physicsBufferPool: BepuSimulation?.BufferPool, specializations: new MeshDataSpecialization[] { 
                        new SurfaceTextureMeshDataSpecialization(new DynamicTexureProvider((gd, rf) => Task.FromResult(TextureFactory.GetDefaultTexture(gd, rf)))),
                        new PhongMaterialMeshDataSpecialization(new PhongMaterialInfo(shininess: .2f, shininessStrength: .1f )) });


            if (BepuSimulation != null)
                CurrentScene.AddUpdateables(new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, plane));
            await CurrentScene.AddCullRenderablesAsync(plane);
        }

        private async Task LoadInstancedAsync()
        {
            Debug.Assert(CurrentScene != null);

            var instances = Enumerable
                .Range(0, 10)
                .Select(x => Enumerable
                    .Range(0, 30)
                    .Select(z => new InstanceInfo
                    {
                        Position = Standard.Random.Noise(new Vector3(x * 3, 0, z * 3), 2f),
                        Rotation = Standard.Random.GetRandomVector(0f, 180f),
                        Scale = Standard.Random.GetRandomVector(0.5f, 1.5f)
                    }))
                .SelectMany(x => x)
                .ToArray();

            var qube = await QubeMesh.CreateAsync(transform: new Transform { Position = new Vector3(30, -2, 0) }, specializations: new[] { new InstancedMeshDataSpecialization(instances) }, name: "qubeInstanced"); 
            var sphere = await SphereMesh.CreateAsync(transform: new Transform { Position = new Vector3(60, -2, 0) }, specializations: new[] { new InstancedMeshDataSpecialization(instances) }, name: "sphereInstanced");
            await CurrentScene.AddCullRenderablesAsync(qube, sphere);
        }

        private async Task LoadXYZLineModelsAsync()
        {
            Debug.Assert(CurrentScene != null);

            var lineLength = 10000f;
            await CurrentScene.AddCullRenderablesAsync(
                await LineMesh.CreateAsync(Vector3.Zero, Vector3.UnitX * lineLength, transform: new Transform { Position = Vector3.Zero }, red: 1f, name: "lineXPositive"),
                await LineMesh.CreateAsync(Vector3.Zero, -Vector3.UnitX * lineLength, transform: new Transform { Position = Vector3.Zero }, red: .5f, name: "lineXNegative"),
                await LineMesh.CreateAsync(Vector3.Zero, Vector3.UnitY * lineLength, transform: new Transform { Position = Vector3.Zero }, green: 1f, name: "lineYPositive"),
                await LineMesh.CreateAsync(Vector3.Zero, -Vector3.UnitY * lineLength, transform: new Transform { Position = Vector3.Zero }, green: .5f, name: "lineYNegative"),
                await LineMesh.CreateAsync(Vector3.Zero, Vector3.UnitZ * lineLength, transform: new Transform { Position = Vector3.Zero }, blue: 1f, name: "linZPositive"),
                await LineMesh.CreateAsync(Vector3.Zero, -Vector3.UnitZ * lineLength, transform: new Transform { Position = Vector3.Zero }, blue: .5f, name: "lineZNegative"));
        }

        //private async Task LoadDaeModelAsync()
        //{
        //    //TODO: make this work
        //    var daeMeshProvider = await DaeFileReader.BinaryMeshFromFileAsync(@"C:\Users\FTR\Documents\tunnel.dae");
        //    var definedMeshProviders = daeMeshProvider.Meshes.Select(provider => provider
        //        .Define<VertexPositionNormalTextureColor, Index32>(data => VertexPositionNormalTextureColor.Build(data, provider.VertexLayout))
        //        .MutateVertices(vertex => new VertexPositionNormalTextureColor(Vector3.Transform(vertex.Position, provider.Transform * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90))), vertex.Color, vertex.TextureCoordinate, vertex.Normal)))
        //        .ToArray();//.Combine();

        //    var flattenedProviders = definedMeshProviders.FlattenMeshDataProviders(daeMeshProvider.Instaces);
        //    var models = flattenedProviders.Select(definedMeshProvider => MeshRenderer.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, definedMeshProvider, new Transform { Position = new Vector3(0, 500, 0), Scale = Vector3.One * 20 }, textureView: TextureFactory.GetDefaultTexture(TextureUsage.Sampled))).ToArray();

        //    CurrentScene.AddCullRenderables(await AssimpDaeModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\tunnel.dae", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(500, 1000, 0), Scale = Vector3.One * 20, Rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)) } }));


        //    //var triangles = daeMeshProvider.SelectMany(x => x.GetTriangles()).ToArray();
        //    //var triangleMeshProvider = new MeshDataProvider<VertexPositionColorNormalTexture, Index32>(
        //    //    triangles.SelectMany(x => new[] { x.A, x.B, x.C }).Select(x => new VertexPositionColorNormalTexture(x, RgbaFloat.Blue)).ToArray(), Enumerable.Range(0, triangles.Length * 3).Select(x => (Index32)x).ToArray(), PrimitiveTopology.TriangleList);
        //    //var colliderModel = Model.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, shaders, triangleMeshProvider, textureView: emptyTexture);
        //    //colliderModel.FillMode.Value = PolygonFillMode.Wireframe;
        //    //GraphicsSystem.AddModels(colliderModel);

        //    //var shape = daeMeshProvider.CombineVertexPosition32Bit().GetPhysicsMesh(Simulation.BufferPool);
        //    //var colliderBehavoir = new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(Simulation, models, shape: shape);
        //    //models.First().AddBehavoirs(colliderBehavoir);
        //    CurrentScene.AddCullRenderables(models);
        //}

        ////private void LoadPhysicObjects()
        ////{
        ////    for (var j = -10; j < 10; j++)
        ////    {
        ////        for (var i = -5; i < 5; i++)
        ////        {
        ////            var m = SphereModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem,
        ////                shaders, texture: emptyTexture, sectorCount: 25, stackCount: 25, red: 1, blue: 1, green: 1,
        ////                creationInfo: new ModelCreationInfo { Position = new Vector3(i * 5, i + 5 + 15 + j + 5 + 50 * 3, j * 5) },
        ////                name: $"physicsSphere{j}:{i}").AddBehavoirs(x => new BepuPhysicsCollidableBehavoir<Sphere>(Simulation, x, bodyType: BepuPhysicsBodyType.Dynamic));
        ////            m.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;
        ////            CurrentScene.AddCullRenderables(m);
        ////        }
        ////    }
        ////}

        private async Task LoadLargeModelsAsync()
        {
            Debug.Assert(CurrentScene != null);
            Debug.Assert(AssimpDaeModelImporter != null);
            Debug.Assert(ObjModelImporter != null);

            // currently the obj file doens doesn't support mtlib file names with spaces and the mtl file does not support map_Ks values (released version)
            var modelLoaders = new List<Task<MeshRenderer[]>>();
            modelLoaders.Add(AssimpDaeModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene 3.dae", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(1000, 100, 0) } }/*, ssvv*/));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(-1000, 100, 0) } }));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/sponza.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(0, 100, -1000), Scale = Vector3.One * 0.1f } }));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(@"resources/models/Space Station Scene dark.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(0, 100, 1000) } }));

            var completedModels = await Task.WhenAll(modelLoaders);
            await CurrentScene.AddCullRenderablesAsync(completedModels.SelectMany(x => x).ToArray());
        }

        ////private async Task LoadCarsAsync(TextureView texture)
        ////{
        ////    var carModels = new List<(MeshDataProvider<VertexPositionColorNormalTexture, Index32>[] Model, Vector3 Forward, Vector3 Up)>();

        ////    //TODO: make those work
        ////    carModels.Add((Model: await DaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae"), Forward: new Vector3(1, 0, 0), Up: new Vector3(0, 0, 1)));
        ////    //carModels.Add((Model: await AssimpDaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae"), Forward: new Vector3(1, 0, 0), Up: new Vector3(0, 0, 1)));

        ////    //TODO: use same mesh data and buffer just different difuse texture (cords)
        ////    var objMesh = await ObjModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car_base.obj");
        ////    //var objMesh = await AssimpDaeModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\car.dae");
        ////    //var newColors = new[] { RgbaFloat.Red, RgbaFloat.Green, RgbaFloat.Yellow, RgbaFloat.Orange, RgbaFloat.Pink, RgbaFloat.Blue, RgbaFloat.Green, RgbaFloat.White, RgbaFloat.DarkRed, RgbaFloat.CornflowerBlue };
        ////    //carModels.AddRange(CreateColoredCarMeshes(objMesh, newColors).Select(providers => (Model: providers, Forward: new Vector3(-1, 0, 0), Up: Vector3.UnitY)).ToArray());

        ////    for (var i = 0; i < 20; i++)
        ////    {
        ////        var meshIndex = i;
        ////        while (meshIndex >= carModels.Count)
        ////        {
        ////            meshIndex -= carModels.Count;
        ////        }
        ////        var mesh = carModels[meshIndex];
        ////        var creationInfo = new ModelCreationInfo { Position = new Vector3(20 * i, -985, 10 * i) };

        ////        // TODO support different up then desired up
        ////        var carConfig = new CarConfiguration
        ////        {
        ////            DriveType = (CarDriveType)Standard.Random.GetRandomNumber(0, 3),
        ////            ForwardSpeed = Standard.Random.GetRandomNumber(160, 200),
        ////            BackwardSpeed = Standard.Random.GetRandomNumber(70, 90),
        ////            Mass = Standard.Random.GetRandomNumber(80, 120),
        ////            Forward = mesh.Forward,
        ////            DesiredForward = -Vector3.UnitX,
        ////            Up = mesh.Up,
        ////            DesiredUp = GraphicsSystem.Camera.Value!.Up,
        ////            CenterOffset = mesh.Forward * 2.7f + mesh.Up * -0.75f,
        ////            AngluarIncrease = 1.25f,
        ////            AngluarMax = .95f,
        ////            SuspensionDirection = -GraphicsSystem.Camera.Value!.Up.Value,
        ////            SuspensionLength = 0.01f,
        ////            CarLength = 3.3f,
        ////            CarWidth = 2.1f,
        ////            WheelLength = 0.8f,
        ////            WheelMass = 3f,
        ////            WheelRadius = 0.9f
        ////        };

        ////        cars.Add(Car.Create(CurrentScene, GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, collidableProperties, carConfig, mesh.Model, shaders, texture, creationInfo, deviceBufferPool, commandListPool));
        ////    }
        ////}

        //private DefinedMeshDataProvider<VertexPositionNormalTextureColor, Index32>[][] CreateColoredCarMeshes(DefinedMeshDataProvider<VertexPositionNormalTextureColor, Index32>[] providers, RgbaFloat[] newColors)
        //    => newColors.Select(newColor => providers.Select(provider => provider.Vertices.First().Color == RgbaFloat.Black ? ChangeCarColor(provider, newColor) : provider).ToArray()).ToArray();


        //private DefinedMeshDataProvider<VertexPositionNormalTextureColor, Index32> ChangeCarColor(DefinedMeshDataProvider<VertexPositionNormalTextureColor, Index32> provider, RgbaFloat newColor)
        //{
        //    return new MeshDataProvider<VertexPositionNormalTextureColor, Index32>(
        //        provider.Vertices.Select(vertex => new VertexPositionNormalTextureColor(vertex.Position, newColor, vertex.TextureCoordinate, vertex.Normal)).ToArray(), provider.Indices,
        //        provider.PrimitiveTopology, provider.MaterialName, provider.TexturePath, provider.AlphaMapPath, provider.Material);
        //}

        //private async Task LoadFloorAsync(TextureView texture)
        //{
        //    var modelMesh = await ObjModelImporter.PositionColorNormalTexture32BitMeshFromFileAsync(@"C:\Users\FTR\Documents\terrain001.obj");
        //    var triangles = modelMesh.Meshes.FlattenMeshDataProviders(modelMesh.Instaces).SelectMany(x => x.GetTriangles()).ToArray();
        //    var meshProvider = new DefinedMeshDataProvider<VertexPositionNormalTextureColor, Index32>(
        //        triangles.SelectMany(x => new[] { x.A, x.B, x.C }).Select(x => new VertexPositionNormalTextureColor(x, RgbaFloat.Red)).ToArray(), Enumerable.Range(0, triangles.Length * 3).Select(x => (Index32)x).ToArray(), PrimitiveTopology.TriangleList);
        //    var colliderModel = MeshRenderer.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, meshProvider, textureView: texture);
        //    colliderModel.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;
        //    CurrentScene.AddCullRenderables(colliderModel);

        //    var floorModel = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\terrain001.obj", new ModelLoadOptions { Transform = new Transform { Position = GraphicsSystem.Camera.Value.Up.Value * -8f }, PhysicsBufferPool = BepuSimulation?.BufferPool });
        //    floorModel[0].MeshBuffer.TextureView.Value = texture;
        //    CurrentScene.AddUpdateables(floorModel.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
        //    CurrentScene.AddCullRenderables(floorModel);
        //}

        //private async Task LoadRacingTracksAsync()
        //{
        //    var track = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\track001.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(10, 0, -25) }, PhysicsBufferPool = BepuSimulation?.BufferPool });
        //    CurrentScene.AddUpdateables(track.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
        //    CurrentScene.AddCullRenderables(track);

        //    var track2 = await ObjModelImporter.ModelFromFileAsync(@"C:\Users\FTR\Documents\track002.obj", new ModelLoadOptions { Transform = new Transform { Position = new Vector3(10, -25, -25) }, PhysicsBufferPool = BepuSimulation?.BufferPool });
        //    CurrentScene.AddUpdateables(track2.Select(x => new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(BepuSimulation, x)).ToArray());
        //    CurrentScene.AddCullRenderables(track2);
        //}

        //private void LoadParticleSystem()
        //{
        ////    var computeParticleShader = ResourceFactory.CreateFromSpirv(new ShaderDescription(
        ////        ShaderStages.Compute,
        ////        File.ReadAllBytes(@"resources\shaders\particle_compute.glsl"),
        ////        entryPoint: "main", ApplicationContext.IsDebug));
        ////    var shaders = ResourceFactory.CreateFromSpirv(
        ////        new ShaderDescription(
        ////            ShaderStages.Vertex,
        ////            File.ReadAllBytes(@"resources\shaders\particle_vertex.glsl"),
        ////            "main"),
        ////        new ShaderDescription(
        ////            ShaderStages.Fragment,
        ////            File.ReadAllBytes(@"resources\shaders\particle_fragment.glsl"),
        ////            "main"));
        ////    var initialParticles = new ParticleInfo[ParticleSystem.MaxParticles];
        ////    for (int i = 0; i < ParticleSystem.MaxParticles; i++)
        ////    {
        ////        ParticleInfo pi = new ParticleInfo(
        ////            new Vector3(40, 20, 40),
        ////            new Vector3(Standard.Random.GetRandomNumber(5f, 50f), Standard.Random.GetRandomNumber(5f, 50f), Standard.Random.GetRandomNumber(5f, 50f)),
        ////            new Vector3(Standard.Random.GetRandomNumber(0.1f, 1f), Standard.Random.GetRandomNumber(0.1f, 1f), Standard.Random.GetRandomNumber(0.1f, 1f)),
        ////            new Vector4(Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f), Standard.Random.GetRandomNumber(0.4f, 0.6f)));
        ////        initialParticles[i] = pi;
        ////    }
        ////    particleSystem = new ParticleSystem(GraphicsDevice, ResourceFactory, GraphicsSystem, computeParticleShader, shaders, initialParticles);
        //}
        //private void PlayLoadingAudio()
        //{
        //    _ = Task.Run(async () =>
        //    {
        //        var loadingContext = AudioSystem.PlayWav(dashRunner);

        //        while (loadingContext.Volume >= 10)
        //        {
        //            loadingContext.Volume -= (int)(loadingContext.Volume / 5f);
        //            await Task.Delay(300);
        //        }

        //        AudioSystem.PlayWav(dashRunner, volume: 55);
        //        AudioSystem.PlayWav(dashRunner, volume: 25);
        //    });
        //}
    }

    //public class Wheel
    //{
    //    public Vector3 SuspensionDirection { get; init; }
    //    public AngularHinge Hinge { get; init; }
    //    public ConstraintHandle HindgeHandle { get; init; }
    //    public ConstraintHandle MotorHandle { get; init; }
    //    public ConstraintHandle LinearAxisServoHandle { get; init; }
    //    public ConstraintHandle PointOnLineServoHandle { get; init; }
    //    public BepuPhysicsCollidableBehavoir<Cylinder> CollidableBehavoir { get; init; }
    //    public MeshRenderer Model { get; init; }

    //    public Wheel(Vector3 suspensionDirection, AngularHinge hinge, ConstraintHandle hindgeHandle, ConstraintHandle motorHandle, ConstraintHandle linearAxisServoHandle, ConstraintHandle pointOnLineServoHandle, BepuPhysicsCollidableBehavoir<Cylinder> collidableBehavoir, MeshRenderer model)
    //    {
    //        SuspensionDirection = suspensionDirection;
    //        Hinge = hinge;
    //        HindgeHandle = hindgeHandle;
    //        MotorHandle = motorHandle;
    //        LinearAxisServoHandle = linearAxisServoHandle;
    //        PointOnLineServoHandle = pointOnLineServoHandle;
    //        CollidableBehavoir = collidableBehavoir;
    //        Model = model;
    //    }
    //}
    //public enum CarDriveType
    //{
    //    Forward,
    //    Backward,
    //    All
    //}
    //public class CarConfiguration
    //{
    //    public CarDriveType DriveType { get; init; } = CarDriveType.All;
    //    public float ForwardSpeed { get; init; } = 180f;
    //    public float BackwardSpeed { get; init; } = 80f;
    //    public float Mass { get; init; } = 100f;
    //    public float CarWidth { get; init; } = 2.1f;
    //    public float CarLength { get; init; } = 3.5f;
    //    public Vector3 CenterOffset { get; init; } = Vector3.UnitZ * 2.6f;
    //    public float AngluarIncrease { get; init; } = 1.25f;
    //    public float AngluarMax { get; init; } = 1f;
    //    public float WheelRadius { get; init; } = 1.2f;
    //    public float WheelLength { get; init; } = 0.8f;
    //    public float WheelMass { get; init; } = 3f;
    //    public float SuspensionLength { get; init; } = 0;
    //    public Vector3 SuspensionDirection { get; init; } = -Vector3.UnitY;
    //    public Vector3 Up { get; init; } = Vector3.UnitY;
    //    public Vector3 Forward { get; init; } = Vector3.UnitZ;

    //    public Vector3 DesiredUp { get; init; } = Vector3.UnitY;
    //    public Vector3 DesiredForward { get; init; } = Vector3.UnitY;
    //}
    //public class Car : IDisposable
    //{
    //    public readonly Transform Transform;
    //    public readonly IReadOnlyCollection<MeshRenderer> Models;
    //    public readonly CarConfiguration Configuration;

    //    private readonly Simulation simulation;
    //    private readonly Font font = SystemFonts.Families.First().CreateFont(50f);
    //    private readonly TextModelBuffer textModelBuffer;
    //    private readonly BepuPhysicsCollidableBehavoir<BepuPhysicsMesh> behavoir;
    //    private readonly Wheel[] wheels;

    //    private float previousSpeed = 0f;
    //    private float previousSteeringAngle = 0f;

    //    private Car(Simulation simulation, MeshRenderer[] models, BepuPhysicsCollidableBehavoir<BepuPhysicsMesh> behavoir, Wheel[] wheels, CarConfiguration carConfiguration, Transform transform, TextModelBuffer textModelBuffer)
    //    {
    //        this.Models = models;
    //        this.Transform = transform;
    //        this.Configuration = carConfiguration;

    //        this.textModelBuffer = textModelBuffer;
    //        this.behavoir = behavoir;
    //        this.wheels = wheels;
    //        this.simulation = simulation;
    //    }

    //    private static Wheel CreateWheel(
    //        Scene scene, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, 
    //        CollidableProperty<SubgroupCollisionFilter> collidableProperty, CarConfiguration carConfiguration, 
    //        in BodyHandle bodyHandle, in RigidPose bodyPose, in AngularHinge hindge, in Vector3 bodyToWheelSuspension, in Vector3 left,
    //        TextureView wheelTexture, DeviceBufferPool? deviceBufferPool = null)
    //    {
    //        var wheelShape = new Cylinder(carConfiguration.WheelRadius, carConfiguration.WheelLength);
    //        var inertia = wheelShape.ComputeInertia(carConfiguration.WheelMass);

    //        RigidPose wheelPose;
    //        // do we need those transforms ? (it is constrained by the springs)
    //        RigidPose.Transform(bodyToWheelSuspension + carConfiguration.SuspensionDirection * carConfiguration.SuspensionLength, bodyPose, out wheelPose.Position);
    //        QuaternionEx.ConcatenateWithoutOverlap(QuaternionEx.CreateFromRotationMatrix(
    //            Matrix.CreateFromAxisAngle(left, MathHelper.ToDegrees(90)) * 
    //            Matrix.CreateFromAxisAngle(carConfiguration.DesiredUp, MathHelper.ToDegrees(90))), 
    //            bodyPose.Orientation, out wheelPose.Orientation);

    //        var transform = new Transform { Position = wheelPose.Position, Rotation = Matrix4x4.CreateFromQuaternion(wheelPose.Orientation), Scale = new Vector3(carConfiguration.WheelRadius * 2, carConfiguration.WheelLength, carConfiguration.WheelRadius * 2) };
    //        var model = SphereMesh.Create(graphicsDevice, resourceFactory, graphicsSystem, transform: transform, sectorCount: 12, stackCount: 12, texture: wheelTexture, deviceBufferPool: deviceBufferPool, radius: carConfiguration.WheelRadius / 2f);
    //        model.Name = "wheel";
    //        model.MeshBuffer.FillMode.Value = PolygonFillMode.Wireframe;

    //        var behavoir = new BepuPhysicsCollidableBehavoir<Cylinder>(simulation, model, inertia, BepuPhysicsBodyType.Dynamic, shape: wheelShape);
    //        var motor = simulation.Solver.Add(behavoir.Collider.BodyHandle!.Value, bodyHandle, new AngularAxisMotor
    //        {
    //            LocalAxisA = carConfiguration.DesiredUp,
    //            Settings = default,
    //            TargetVelocity = default
    //        });
    //        var linearAxisServo = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, new LinearAxisServo
    //        {
    //            LocalPlaneNormal = carConfiguration.SuspensionDirection,
    //            TargetOffset = carConfiguration.SuspensionLength,
    //            LocalOffsetA = bodyToWheelSuspension,
    //            LocalOffsetB = default,
    //            ServoSettings = ServoSettings.Default,
    //            SpringSettings = new SpringSettings(5f, 0.4f)
    //        });
    //        var pointOnLineServo = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, new PointOnLineServo
    //        {
    //            LocalDirection = carConfiguration.SuspensionDirection,
    //            LocalOffsetA = bodyToWheelSuspension,
    //            LocalOffsetB = default,
    //            ServoSettings = ServoSettings.Default,
    //            SpringSettings = new SpringSettings(30, 0.5f)
    //        });
    //        var hindgeHandle = simulation.Solver.Add(bodyHandle, behavoir.Collider.BodyHandle!.Value, hindge);

    //        ref var wheelProperties = ref collidableProperty.Allocate(behavoir.Collider.BodyHandle!.Value);
    //        wheelProperties = new SubgroupCollisionFilter { GroupId = bodyHandle.Value };

    //        scene.AddUpdateables(behavoir);
    //        scene.AddCullRenderables(model);
    //        return new Wheel(carConfiguration.SuspensionDirection, hindge, hindgeHandle, motor, linearAxisServo, pointOnLineServo, behavoir, model);
    //    }

    //    public static Car Create(
    //        Scene scene, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, 
    //        CollidableProperty<SubgroupCollisionFilter> collidableProperty, CarConfiguration carConfiguration, 
    //        SpecializedMeshDataProvider[] meshDataProviders, TextureView texture, Transform transform, 
    //        DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    //    {
    //        var left = Vector3.Normalize(Vector3.One - Vector3.Abs(carConfiguration.Up) - Vector3.Abs(carConfiguration.Forward) * carConfiguration.Forward.Length());

    //        // TODO: apply transform before creating model
    //        var angle = (float)Math.Acos(Vector3.Dot(carConfiguration.DesiredForward, carConfiguration.Forward) / (carConfiguration.DesiredForward.Length() * carConfiguration.Forward.Length()));
    //        var realTransform = transform with { Rotation = transform.Rotation * Matrix4x4.CreateFromAxisAngle(carConfiguration.Up, angle) };

    //        var buffers = meshDataProviders.Select(meshData => MeshDeviceBuffer.Create(graphicsDevice, resourceFactory, meshData, textureView: texture, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool)).ToArray();
    //        var models = buffers.Select(buffer => new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, buffer, transform: realTransform, name: "car")).ToArray();
    //        scene.AddCullRenderables(models);

    //        var shape = meshDataProviders.CombineVertexPosition32Bit().GetPhysicsMesh(simulation.BufferPool);
    //        var colliderBehavoir = new BepuPhysicsCollidableBehavoir<BepuPhysicsMesh>(simulation, models, carConfiguration.Mass, BepuPhysicsBodyType.Dynamic, shape: shape);
    //        var body = simulation.Bodies[colliderBehavoir.Collider.BodyHandle ?? throw new Exception()];
    //        ref var bodyProperties = ref collidableProperty.Allocate(body.Handle);
    //        bodyProperties = new SubgroupCollisionFilter { GroupId = body.Handle.Value };
    //        scene.AddUpdateables(colliderBehavoir);


    //        // TODO: do depending on up
    //        QuaternionEx.TransformUnitY(QuaternionEx.CreateFromAxisAngle(carConfiguration.Forward, MathF.PI * 0.5f), out var wheelAxis);
    //        var hindge = new AngularHinge
    //        {
    //            LocalHingeAxisA = wheelAxis,
    //            LocalHingeAxisB = carConfiguration.Up,
    //            SpringSettings = new SpringSettings(30, 1)
    //        };
    //        var handles1 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, carConfiguration.Forward * carConfiguration.CarLength + left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
    //        var handles2 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, carConfiguration.Forward * carConfiguration.CarLength - left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
    //        var handles3 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, -carConfiguration.Forward * carConfiguration.CarLength + left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);
    //        var handles4 = CreateWheel(scene, graphicsDevice, resourceFactory, graphicsSystem, simulation, collidableProperty, carConfiguration, body.Handle, body.Pose, hindge, -carConfiguration.Forward * carConfiguration.CarLength - left * carConfiguration.CarWidth + carConfiguration.CenterOffset, left, texture, deviceBufferPool);

    //        var createTextBufferBehavoir = new Action<MeshRenderer>(meshRenderer => scene.AddUpdateables(new AlwaysFaceCameraBehavior(meshRenderer, Matrix4x4.CreateRotationX(MathHelper.ToRadians(90)))));
    //        var textBuffer = new TextModelBuffer(scene, graphicsDevice, resourceFactory, graphicsSystem, deviceBufferPool, commandListPool, createTextBufferBehavoir);
    //        textBuffer.SetTransform(scale: Vector3.One * 0.05f);

    //        return new Car(simulation, models, colliderBehavoir, new[] { handles1, handles2, handles3, handles4 }, carConfiguration, realTransform, textBuffer);
    //    }

    //    public void Reset()
    //    {
    //        var body = behavoir.Collider.GetBodyReference();
    //        body.Velocity = Vector3.Zero;
    //        foreach (var model in Models)
    //        {
    //            model.Transform.Value = Transform;
    //        }
    //        foreach (var wheel in wheels)
    //        {
    //            wheel.Model.Transform.Value = wheel.Model.Transform.Value with { Position = Transform.Position };
    //        }
    //    }

    //    public void Update(InputHandler inputHandler, float delta)
    //    {
    //        var speed = 0f;
    //        var angle = 0f;
    //        if (inputHandler.IsKeyDown(Key.Up))
    //        {
    //            speed = Configuration.ForwardSpeed;
    //        }
    //        if (inputHandler.IsKeyDown(Key.Down))
    //        {
    //            speed = -Configuration.BackwardSpeed;
    //        }

    //        if (inputHandler.IsKeyDown(Key.Left))
    //        {
    //            angle = Math.Min(previousSteeringAngle + Configuration.AngluarIncrease * delta, Configuration.AngluarMax);
    //        }
    //        else if (inputHandler.IsKeyDown(Key.Right))
    //        {
    //            angle = Math.Max(previousSteeringAngle - Configuration.AngluarIncrease * delta, -Configuration.AngluarMax);
    //        }
    //        else
    //        {
    //            angle = previousSteeringAngle >= Configuration.AngluarIncrease * delta
    //                ? previousSteeringAngle - Configuration.AngluarIncrease * delta
    //                : previousSteeringAngle <= -Configuration.AngluarIncrease * delta
    //                ? previousSteeringAngle + Configuration.AngluarIncrease * delta
    //                : 0;
    //        }

    //        if (previousSpeed != speed)
    //        {
    //            var motoredWheels = Configuration.DriveType == CarDriveType.All ? wheels :
    //                                Configuration.DriveType == CarDriveType.Forward ? wheels.Take(2) :
    //                                wheels.Skip(2);

    //            foreach (var wheel in motoredWheels)
    //            {
    //                simulation.Solver.ApplyDescription(wheel.MotorHandle, new AngularAxisMotor
    //                {
    //                    LocalAxisA = -Configuration.DesiredUp,
    //                    Settings = new MotorSettings(100, 1e-6f),
    //                    TargetVelocity = speed
    //                });
    //            }
    //            previousSpeed = speed;
    //        }

    //        if (previousSteeringAngle != angle)
    //        {
    //            foreach (var wheel in wheels.Take(2))
    //            {
    //                var steeredHinge = wheel.Hinge;
    //                Matrix3x3.CreateFromAxisAngle(Configuration.SuspensionDirection, -angle, out var rotation);
    //                Matrix3x3.Transform(wheel.Hinge.LocalHingeAxisA, rotation, out steeredHinge.LocalHingeAxisA);
    //                simulation.Solver.ApplyDescription(wheel.HindgeHandle, steeredHinge);
    //            }
    //            previousSteeringAngle = angle;
    //        }

    //        var mainModel = Models.First();
    //        var body = behavoir.Collider.GetBodyReference();

    //        // TODO update only if is cullable?
    //        var speedText = Math.Round(body.Velocity.Linear.Length(), 2).ToString("0.00");
    //        var text = $"{speedText} m/s";
    //        textModelBuffer.Write(font, text, RgbaFloat.Black);
    //        textModelBuffer.SetTransform(position: mainModel.Transform.Value.Position + Configuration.DesiredUp * 5f);
    //    }

    //    public void Dispose()
    //    {
    //        foreach(var model in this.Models)
    //        {
    //            model.Dispose();
    //        }
    //    }
    //}
    //class SampleContactEventHandler : IContactEventHandler
    //{
    //    private const string contactSound = @"resources/audio/contact.wav";

    //    private readonly Game game;
    //    private readonly IAudioSystem audioSystem;
    //    private readonly ILogger<SampleContactEventHandler> logger = ApplicationContext.LoggerFactory.CreateLogger<SampleContactEventHandler>();

    //    public SampleContactEventHandler(Game game, IAudioSystem audioSystem)
    //    {
    //        this.game = game;
    //        this.audioSystem = audioSystem;
    //        this.audioSystem.PreLoadWav(contactSound);
    //    }

    //    public void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
    //    {
    //        const int maxVolume = 60;
    //        const float maxIntensity = 16;

    //        if (manifold.Count <= 0)
    //            return;

    //        manifold.GetContact(0, out var offset, out _, out var depth, out _);
    //        var volume = maxVolume / 2f * Math.Min(2f, Math.Abs(depth));

    //        if (volume > 3f)
    //        {
    //            var dynamicBody = pair.A.Mobility == CollidableMobility.Dynamic ? pair.A : pair.B;
    //            var body = game.BepuSimulation.Bodies.GetBodyReference(dynamicBody.BodyHandle);
    //            var position = offset + body.Pose.Position;
    //            logger.LogInformation($"A colision with a deth of {depth} occured which will create a sound with a volume of {volume} at {position}");
    //            audioSystem.PlaceWav(contactSound, position, maxIntensity / maxVolume * volume, (int)volume);
    //        }
    //    }
    //}
}

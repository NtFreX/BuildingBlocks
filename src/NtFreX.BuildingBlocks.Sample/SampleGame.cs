using BepuPhysics.CollisionDetection;
using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Audio;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Desktop;
using NtFreX.BuildingBlocks.Models;
using NtFreX.BuildingBlocks.Sample.Models;
using NtFreX.BuildingBlocks.Shell;
using System.Numerics;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Sample
{
    public class SampleGame : Game
    {
        private const string dashRunner = @"resources/audio/Dash Runner.wav";
        private const string detective = @"resources/audio/8-bit Detective.wav";

        private Model[]? models;
        private Model? sun;
        private Model[]? goblin;
        private Model[]? dragon;

        private float sunYawn = 0f;
        private float sunPitch = 0f;

        public SampleGame(IShell shell, ILoggerFactory loggerFactory) 
            : base(shell, loggerFactory) { }

        protected override IContactEventHandler LoadContactEventHandler() => new SampleContactEventHandler(this, AudioSystem);
        protected override void OnUpdating(float delta)
        {
            var sunSpeed = sun!.Position.Value.Y < 0 ? 0.4f : 0.1f;
            var sunDistance = 2000f;

            sunPitch += sunSpeed * delta;
            
            var rotation = Quaternion.CreateFromYawPitchRoll(sunYawn, sunPitch, 0f);
            var lightPos = Vector3.Transform(Vector3.UnitZ, rotation) * sunDistance;
            var brightness = Math.Min(Math.Max((lightPos.Y + (sunDistance / 5)) / sunDistance, 0.05f), .8f);
            GraphicsSystem.LightSystem.AmbientLight = new Vector3(brightness);

            var lights = new List<PointLightInfo>(new[] { new PointLightInfo { Color = new Vector3(.02f, 0, 0), Intensity = Standard.Random.GetRandomNumber(0.05f, 0.2f), Range = 25f, Position = Vector3.Zero } });
            if (sun.Position.Value.Y >= -10) 
            {
                lights.Add(new PointLightInfo { Color = new Vector3(.02f, Math.Min(Math.Max(brightness, .002f), .02f), 0), Intensity = 0.2f, Range = sunDistance * 2, Position = lightPos });
            }
            GraphicsSystem.LightSystem.SetPointLights(lights.ToArray());

            sun.Position.Value = lightPos;

            models![0].Rotation.Value = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateRotationX(sunPitch) * Matrix4x4.CreateRotationY(sunPitch));
            foreach (var model in models)
            {
                model.Material.Value = models[0].Material.Value with { Opacity = sun.Position.Value.Y / sunDistance };
            }
            models[models.Length - 1].Material.Value = models[models.Length - 1].Material.Value with { Opacity = 1f };

            foreach (var model in goblin!.Concat(dragon!))
            {
                model.FillMode.Value = sun.Position.Value.Z > 0 ? PolygonFillMode.Wireframe : PolygonFillMode.Solid;
            }
        }

        protected override Camera LoadCamera()
        {
            var camera = new MovableCamera(GraphicsDevice, ResourceFactory, Shell.Width, Shell.Height);
            camera.Position.Value = new Vector3(40, 25, 40);
            return camera;
        }

        protected override async Task LoadResourcesAsync()
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

            TextureFactory.SetEmptyTexture(@"resources/models/textures/empty_texture.png");
            TextureFactory.SetDefaultTexture(@"resources/models/textures/no_texture.png");

            var emptyTexture = await TextureFactory.GetEmptyTextureAsync(TextureUsage.Sampled);

            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                File.ReadAllBytes("resources/shaders/basic.vert"),
                "main", ApplicationContext.IsDebug);
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                File.ReadAllBytes("resources/shaders/basic.frag"),
                "main", ApplicationContext.IsDebug);

            var shaders = ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            var stoneTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/spnza_bricks_a_diff.png", TextureUsage.Sampled);
            var blueTexture = await TextureFactory.GetTextureAsync(@"resources/models/textures/app.png", TextureUsage.Sampled);

            var qube = QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(20, 20, 20) }, shaders, texture: emptyTexture, sideLength: 1, collider: true, dynamic: true);
            qube.FillMode.Value = PolygonFillMode.Wireframe;
            GraphicsSystem.AddModels(qube);

            var qubeSideLength = .5f;
            var lineLength = 10000f;
            models = new Model[] {
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(6, 6, 3) }, shaders, sideLength: 3, texture: stoneTexture, collider: true),

                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),

                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, -4) }, shaders, texture: blueTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, -3) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, blue: 1f, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, -2) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, green: 1f, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, -1) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, red: 1f, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, 1) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, 2) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, 3) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 0, 4) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),

                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, -4, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, -3, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, -2, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, -1, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 1, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 2, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 3, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(0, 4, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),

                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-4, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-3, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-2, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-1, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(1, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(2, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(3, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(4, 0, 0) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                                                                                                                     
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-4, -4, -4) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-3, -3, -3) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-2, -2, -2) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(-1, -1, -1) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(1, 1, 1) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(2, 2, 2) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(3, 3, 3) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),
                QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(4, 4, 4) }, shaders, texture: emptyTexture, sideLength: qubeSideLength, collider: true),

                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, Vector3.UnitX * lineLength, texture: emptyTexture, red: 1f, alpha: 1f),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, -Vector3.UnitX * lineLength, texture: emptyTexture, red: .5f, alpha: 1f),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, Vector3.UnitY * lineLength, texture: emptyTexture, green: 1f, alpha: 1f),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, -Vector3.UnitY * lineLength, texture: emptyTexture, green: .5f, alpha: 1f),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, Vector3.UnitZ * lineLength, texture: emptyTexture, blue: 1f, alpha: 1f),
                LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, Vector3.Zero, -Vector3.UnitZ * lineLength, texture: emptyTexture, blue: .5f, alpha: 1f),

                PlaneModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, 
                    new ModelCreationInfo { Position = GraphicsSystem.Camera.Up.Value * -10f, Scale = Vector3.One * 10f }, 
                    shaders, rows: 250, columns: 300, texture: stoneTexture, material: new MaterialInfo { ShininessStrength = .001f }, collider: true
                ),
            };

            sun = SphereModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.Zero }, shaders, texture: emptyTexture, red: 1f, green: 1, alpha: 1f, radius: 25f, sectorCount: 25, stackCount: 25);

            {
                // TODO: why is this not working?
                var mesh = await DaeModelImporter.MeshFromFileAsync(@"resources/models/chinesedragon.dae");
                var data = MeshDeviceBuffer.Create(GraphicsDevice, ResourceFactory, mesh[0], textureView: emptyTexture);
                GraphicsSystem.AddModels(Enumerable.Range(0, 100).Select(x => new Model(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = Vector3.One * x * 2 + Vector3.UnitY * 50 }, shaders, data, collider: false, name: $"goblin{x}")).ToArray());
            }
            {
                //TODO: test instanced bounding boxes with rotation and scale
                var convertingMesh = QubeModel.CreateMesh();
                var data = MeshDeviceBuffer.Create(GraphicsDevice, ResourceFactory, convertingMesh, textureView: emptyTexture);
                for (var z = 0; z < 100; z++)
                {
                    var instances = Enumerable.Range(0, 1000).Select(x => new InstanceInfo { Position = new Vector3(x * 2, 0, 0) }).ToArray();
                    var model = new Model(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(30, -10, z * 2) }, shaders, data, collider: false, name: "qubeInstanced", instances: instances);
                    //CreateBoundingBox(model.GetBoundingBox(), shaders, emptyTexture);
                    GraphicsSystem.AddModels(model);
                }
            }

            //_ = Task.Run(async () =>
            //{
            //    var gobilinMesh = await DaeModelImporter.MeshFromFileAsync(@"resources/models/goblin.dae");
            //    var buffers = gobilinMesh[0].BuildVertexAndIndexBuffer(GraphicsDevice, ResourceFactory);
            //    var convertingMesh = gobilinMesh[0] as MeshDataProvider<VertexPositionColorNormalTexture, uint> ?? throw new Exception();
            //    var triangleMesh = new TriangleMeshDataProvider<VertexPositionColorNormalTexture, uint>(
            //        convertingMesh.GetTriangles(), convertingMesh.Vertices, convertingMesh.Indices, convertingMesh.IndexFormat,
            //        VertexPositionColorNormalTexture.VertexLayout, convertingMesh.MaterialName, convertingMesh.TexturePath,
            //        VertexPositionColorNormalTexture.BytesBeforePosition, convertingMesh.Material);

            //    await triangleMesh.ToFileAsync("goblin.dat");
            //    var readMesh = await TriangleMeshDataProvider<VertexPositionColorNormalTexture, uint>.FromFileAsync("goblin.dat");
            //    //var buffer = new MeshDeviceBuffer()
            //    GraphicsSystem.AddModels(Model.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo(), shaders, readMesh, readMesh.VertexLayout, readMesh.IndexFormat, readMesh.PrimitiveTopology, emptyTexture));
            //});

            goblin = await DaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, -15), Scale = new Vector3(.001f) }, shaders, @"resources/models/goblin.dae");
            foreach (var g in goblin)
            {
                g.Material.Value = g.Material.Value with { Opacity = 1f };
            }
            dragon = await DaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(10, 0, 15) }, shaders, @"resources/models/chinesedragon.dae");

            // currently the obj file doens doesn't support mtlib file names with spaces and the mtl file does not support map_Ks values (released version)
            var modelLoaders = new List<Task<Model[]>>();
            modelLoaders.Add(DaeModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(1000, 100, 0), Rotation = Quaternion.CreateFromYawPitchRoll(0, -1.5f, 0) }, shaders, @"resources/models/Space Station Scene 3.dae"/*, ssvv*/));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(-1000, 100, 0) }, shaders, @"resources/models/Space Station Scene.obj"));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(0, 100, -1000), Scale = Vector3.One * 0.1f }, shaders, @"resources/models/sponza.obj"));
            modelLoaders.Add(ObjModelImporter.ModelFromFileAsync(new ModelCreationInfo { Position = new Vector3(0, 100, 1000) }, shaders, @"resources/models/Space Station Scene dark.obj"));

            var ballModels = new List<Model>();
            for (var j = -10; j < 10; j++)
            {
                for (var i = -5; i < 5; i++)
                {
                    var m = SphereModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(i * 5, i + 5 + 15 + j + 5 + 50 * 3, j * 5) }, shaders, texture: emptyTexture, sectorCount: 25, stackCount: 25, collider: true, dynamic: true);
                    m.FillMode.Value = PolygonFillMode.Wireframe;
                    ballModels.Add(m);
                }
            }

            var completedModels = await Task.WhenAll(modelLoaders);
            //foreach (var model in completedModels.SelectMany(x => x).Concat(dragon).Concat(goblin).Concat(models).Concat(ballModels))
            //{
            //    CreateBoundingBox(model.GetBoundingBox(), shaders, emptyTexture);
            //}

            GraphicsSystem.AddModels(completedModels.SelectMany(x => x).ToArray());
            GraphicsSystem.AddModels(ballModels.ToArray());
            GraphicsSystem.AddModels(models);
            GraphicsSystem.AddModels(sun);
            GraphicsSystem.AddModels(goblin);
            GraphicsSystem.AddModels(dragon);

            AudioSystem.StopAll();
            AudioSystem.PlaceWav(detective, loop: true, position: Vector3.Zero, intensity: 100f);
        }

        private void CreateBoundingBox(BoundingBox boundingBox, Shader[] shaders, TextureView? texture)
        {
            var scaleX = boundingBox.Max.X - boundingBox.Min.X;
            var scaleY = boundingBox.Max.Y - boundingBox.Min.Y;
            var scaleZ = boundingBox.Max.Z - boundingBox.Min.Z;
            var posX = boundingBox.Min.X + scaleX / 2f;
            var posY = boundingBox.Min.Y + scaleY / 2f;
            var posZ = boundingBox.Min.Z + scaleZ / 2f;
            var bounds = QubeModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, Simulation, new ModelCreationInfo { Position = new Vector3(posX, posY, posZ), Scale = new Vector3(scaleX, scaleY, scaleZ) }, shaders, red: 1, texture: texture);
            bounds.Material.Value = bounds.Material.Value with { Opacity = .5f };
            GraphicsSystem.AddModels(bounds);
        }

        protected override void OnRendering(float deleta, CommandList commandList) { }
    }
    class SampleContactEventHandler : IContactEventHandler
    {
        private const string contactSound = @"resources/audio/contact.wav";

        private readonly Game game;
        private readonly SdlAudioSystem audioSystem;
        private readonly ILogger<SampleContactEventHandler> logger = ApplicationContext.LoggerFactory.CreateLogger<SampleContactEventHandler>();

        public SampleContactEventHandler(Game game, SdlAudioSystem audioSystem)
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
                var bodyReference = pair.A.BodyHandle == default ? pair.B : pair.A;
                var body = game.Simulation.Bodies.GetBodyReference(bodyReference.BodyHandle);
                var position = offset + body.Pose.Position;
                logger.LogInformation($"A colision with a deth of {depth} occured which will create a sound with a volume of {volume} at {position}");
                audioSystem.PlaceWav(contactSound, position, maxIntensity / maxVolume * volume, (int)volume);
            }
        }
    }
}

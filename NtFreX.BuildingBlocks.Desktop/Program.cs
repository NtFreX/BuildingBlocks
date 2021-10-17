using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace NtFreX.BuildingBlocks.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var game = new Game(debug);
            game.CreateResources();
            game.Run();
        }
    }

    struct VertexPositionColor
    {
        public Vector3 Position;
        public Vector3 Normal;
        public RgbaFloat Color;

        public VertexPositionColor(Vector3 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
            Normal = Vector3.One;
        }
        public const uint SizeInBytes = 40;
    }

    class Model : IDisposable
    {
        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }
        public IndexFormat IndexFormat { get; private set; } = IndexFormat.UInt16;
        public uint IndexCount { get; private set; }
        public uint VertexCount { get; private set; }

        public Model(GraphicsDevice graphicsDevice, VertexPositionColor[] vertices, ushort[] indices)
        {
            IndexCount = (uint) indices.Length;
            VertexCount = (uint) vertices.Length;

            VertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(VertexCount * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            IndexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(IndexCount * sizeof(ushort), BufferUsage.IndexBuffer));

            graphicsDevice.UpdateBuffer(VertexBuffer, 0, vertices);
            graphicsDevice.UpdateBuffer(IndexBuffer, 0, indices);
        }

        public void Draw(CommandList commandList)
        {
            commandList.SetVertexBuffer(0, VertexBuffer);
            commandList.SetIndexBuffer(IndexBuffer, IndexFormat);
            commandList.DrawIndexed(
                indexCount: IndexCount,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }

    class Game
    {
        private Sdl2Window window;
        private GraphicsDevice graphicsDevice;

        private DeviceBuffer projectionBuffer;
        private DeviceBuffer viewBuffer;
        private DeviceBuffer worldBuffer;
        private DeviceBuffer lightBuffer;

        private CommandList commandList;
        private ResourceSet rs;
        private Shader[] shaders;
        private Pipeline pipeline;

        private MovableCamera camera;
        private Model[] models;

        public Game(bool debug)
        {
            window = VeldridStartup.CreateWindow(new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = Assembly.GetEntryAssembly().FullName
            });
            window.Resized += () =>
            {
                camera.WindowWidth.Value = window.Width;
                camera.WindowHeight.Value = window.Height;

                graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            };

            camera = new MovableCamera(window.Width, window.Height);

            var options = new GraphicsDeviceOptions
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                Debug = debug,
            };

            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);            
        }

        private void Draw()
        {
            graphicsDevice.UpdateBuffer(projectionBuffer, 0, camera.ProjectionMatrix);
            graphicsDevice.UpdateBuffer(viewBuffer, 0, camera.ViewMatrix);

            commandList.Begin();
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Black);

            
            commandList.SetPipeline(pipeline);
            commandList.SetGraphicsResourceSet(0, rs);

            foreach (var model in models)
            {
                model.Draw(commandList);
            }

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.SwapBuffers();
        }

        public void CreateResources()
        {
            var factory = graphicsDevice.ResourceFactory;

            projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            worldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            lightBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            
            Matrix4x4 worldMatrix = Matrix4x4.Identity;
            graphicsDevice.UpdateBuffer(worldBuffer, 0, ref worldMatrix);

            Vector4 light = new Vector4(5, 5, 5, 1);
            graphicsDevice.UpdateBuffer(lightBuffer, 0, ref light);

            ResourceLayout layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("World", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightPos", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            models = new Model[] {
                new Model(graphicsDevice, QubeModel.GetVertices(), QubeModel.GetIndices()),
                new Model(graphicsDevice, QubeModel.GetVertices(new Vector3(1, 1, 1)), QubeModel.GetIndices()),
                new Model(graphicsDevice, QubeModel.GetVertices(new Vector3(2, 2, 2)), QubeModel.GetIndices()),
                new Model(graphicsDevice, QubeModel.GetVertices(new Vector3(3, 3, 3)), QubeModel.GetIndices()),
                new Model(graphicsDevice, QubeModel.GetVertices(new Vector3(4, 4, 4)), QubeModel.GetIndices()),
            };

            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
                new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                File.ReadAllBytes("shaders/basic.vert"),
                "main");
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                File.ReadAllBytes("shaders/basic.frag"),
                "main");

            shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            var pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;

            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: shaders);

            pipelineDescription.Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription;
            pipelineDescription.ResourceLayouts = new[] { layout };

            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);


            rs = factory.CreateResourceSet(new ResourceSetDescription(layout, projectionBuffer, viewBuffer, worldBuffer, lightBuffer));

            commandList = factory.CreateCommandList();
        }

        public void Run()
        {
            Stopwatch sw = Stopwatch.StartNew();
            double previousElapsed = sw.Elapsed.TotalSeconds;

            while (window.Exists)
            {
                double newElapsed = sw.Elapsed.TotalSeconds;
                float deltaSeconds = (float)(newElapsed - previousElapsed);

                var inputSnapshot = window.PumpEvents();
                var inputHandler = new InputHandler(inputSnapshot);
                camera.Update(inputHandler, deltaSeconds);

                Draw();
            }

            DisposeResources();
        }


        private void DisposeResources()
        {
            pipeline.Dispose();
            foreach(var shader in shaders)
            {
                shader.Dispose();
            }
            commandList.Dispose();
            foreach(var model in models)
            {
                model.Dispose();
            }            
            graphicsDevice.Dispose();
        }
    }
}

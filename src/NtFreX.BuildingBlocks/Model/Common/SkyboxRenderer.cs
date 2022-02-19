using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common
{
    public class SkyboxRenderer : Renderable
    {
        private readonly Image<Rgba32> front;
        private readonly Image<Rgba32> back;
        private readonly Image<Rgba32> left;
        private readonly Image<Rgba32> right;
        private readonly Image<Rgba32> top;
        private readonly Image<Rgba32> bottom;

        private DeviceBuffer vb;
        private DeviceBuffer ib;
        private Pipeline pipeline;
        private ResourceSet resourceSet;

        private readonly DisposeCollector disposeCollector = new DisposeCollector();
        private readonly bool isDebug;

        public SkyboxRenderer(
            Image<Rgba32> front, Image<Rgba32> back, Image<Rgba32> left,
            Image<Rgba32> right, Image<Rgba32> top, Image<Rgba32> bottom,
            GraphicsDevice gd, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, CommandList cl, RenderContext sc,
            bool isDebug = false)
        {
            this.front = front;
            this.back = back;
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
            this.isDebug = isDebug;

            // TODO: implement pattern correctly
            CreateDeviceObjects(gd, resourceFactory, graphicsSystem, cl, sc);
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, CommandList cl, RenderContext sc)
        {
            Debug.Assert(graphicsSystem.Camera.Value != null);

            vb = resourceFactory.CreateBuffer(new BufferDescription((uint) (Unsafe.SizeOf<VertexPosition>() * vertices.Length), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(vb, 0, vertices);

            ib = resourceFactory.CreateBuffer(new BufferDescription((uint)(Unsafe.SizeOf<ushort>() * indices.Length), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(ib, 0, indices);

            var imageSharpCubemapTexture = new ImageSharpCubemapTexture(right, left, top, bottom, back, front, false);

            var textureCube = imageSharpCubemapTexture.CreateDeviceTexture(gd, resourceFactory);
            var textureView = resourceFactory.CreateTextureView(new TextureViewDescription(textureCube));

            var vertexLayouts = new VertexLayoutDescription[] 
            {
                new VertexLayoutDescription(new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
            };

            var shaders = ShaderPrecompiler.CompileVertexAndFragmentShaders(gd, resourceFactory, new Dictionary<string, bool>(), new Dictionary<string, string>(), "Resources/skybox", isDebug);

            var layout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("CubeTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CubeSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { shaders[0], shaders[1] }, ShaderPrecompiler.GetSpecializations(gd)),
                new ResourceLayout[] { layout },
                sc.MainSceneFramebuffer.OutputDescription);

            pipeline = resourceFactory.CreateGraphicsPipeline(ref pd);

            // TODO: update when camera changed
            resourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                layout,
                graphicsSystem.Camera.Value.ProjectionBuffer,
                graphicsSystem.Camera.Value.ViewBuffer,
                textureView,
                gd.PointSampler));

            disposeCollector.Add(vb, ib, textureCube, textureView, layout, pipeline, resourceSet, shaders[0], shaders[1]);
        }

        public override void DestroyDeviceObjects()
        {
            disposeCollector.DisposeAll();
        }

        public override void Render(GraphicsDevice gd, CommandList cl, RenderContext sc, RenderPasses renderPass)
        {
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, resourceSet);
            cl.SetVertexBuffer(0, vb);
            cl.SetIndexBuffer(ib, IndexFormat.UInt16);
            cl.DrawIndexed((uint)indices.Length, 1, 0, 0, 0);
        }

        public override RenderPasses RenderPasses => RenderPasses.Standard;
        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition) => new RenderOrderKey(ulong.MaxValue);
        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, RenderContext sc) { }

        private static readonly VertexPosition[] vertices = new VertexPosition[]
        {
            // Top
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            // Bottom
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Left
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Right
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            // Back
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            // Front
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        };

        private static readonly ushort[] indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
    }
}

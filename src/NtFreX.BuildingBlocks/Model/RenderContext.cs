using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh.Factories;
using Veldrid;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Model
{
    public class RenderContext : IDisposable
    {
        private readonly TextureSampleCount mainSceneSampleCount;

        public VeldridTexture? MainSceneColorTexture { get; private set; }
        public VeldridTexture? MainSceneDepthTexture { get; private set; }
        public VeldridTexture? MainSceneResolvedColorTexture { get; private set; }
        public TextureView? MainSceneResolvedColorView { get; private set; }
        public Framebuffer? MainSceneFramebuffer { get; set; }
        public ResourceSet? MainSceneViewResourceSet { get; private set; }
        public ResourceLayout? TextureSamplerResourceLayout { get; private set; }
        public VeldridTexture? DuplicatorTarget0 { get; private set; }
        public TextureView? DuplicatorTargetView0 { get; private set; }
        public ResourceSet? DuplicatorTargetSet0 { get; internal set; }
        public VeldridTexture? DuplicatorTarget1 { get; private set; }
        public TextureView? DuplicatorTargetView1 { get; private set; }
        public ResourceSet? DuplicatorTargetSet1 { get; internal set; }
        public Framebuffer? DuplicatorFramebuffer { get; private set; }
        public ResourceSet? DrawDeltaComputeSet { get; internal set; }
        public DeviceBuffer? DrawDeltaBuffer { get; private set; }

        public CascadedShadowMaps ShadowMaps { get; private set; }
        public TextureView NearShadowMapView => ShadowMaps.NearShadowMapView;
        public TextureView MidShadowMapView => ShadowMaps.MidShadowMapView;
        public TextureView FarShadowMapView => ShadowMaps.FarShadowMapView;
        public Framebuffer NearShadowMapFramebuffer => ShadowMaps.NearShadowMapFramebuffer;
        public Framebuffer MidShadowMapFramebuffer => ShadowMaps.MidShadowMapFramebuffer;
        public Framebuffer FarShadowMapFramebuffer => ShadowMaps.FarShadowMapFramebuffer;
        public VeldridTexture ShadowMapTexture => ShadowMaps.NearShadowMap; // Only used for size.
        public DeviceBuffer LightViewBufferNear { get; internal set; }
        public DeviceBuffer LightViewBufferMid { get; internal set; }
        public DeviceBuffer LightViewBufferFar { get; internal set; }
        public DeviceBuffer LightProjectionBufferNear { get; internal set; }
        public DeviceBuffer LightProjectionBufferMid { get; internal set; }
        public DeviceBuffer LightProjectionBufferFar { get; internal set; }
        public ResourceSet LightProjectionViewSetNear { get; internal set; }
        public ResourceSet LightProjectionViewSetMid { get; internal set; }
        public ResourceSet LightProjectionViewSetFar { get; internal set; }
        public ResourceSet? ShadowResourceSet { get; private set; }
        public DeviceBuffer CascadeInfoBuffer { get; internal set; }

        public RenderContext(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, TextureSampleCount mainSceneSampleCount)
        {
            graphicsDevice.GetPixelFormatSupport(
                PixelFormat.R16_G16_B16_A16_Float,
                TextureType.Texture2D,
                TextureUsage.RenderTarget,
                out PixelFormatProperties properties);

            while (!properties.IsSampleCountSupported(mainSceneSampleCount))
            {
                mainSceneSampleCount = mainSceneSampleCount - 1;
            }

            LightProjectionBufferNear = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightProjectionBufferNear.Name = "LightProjectionBufferNear";
            LightProjectionBufferMid = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightProjectionBufferMid.Name = "LightProjectionBufferMid";
            LightProjectionBufferFar = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightProjectionBufferFar.Name = "LightProjectionBufferFar";
            LightViewBufferNear = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightViewBufferNear.Name = "LightViewBufferNear";
            LightViewBufferMid = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightViewBufferMid.Name = "LightViewBufferMid";
            LightViewBufferFar = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            LightViewBufferFar.Name = "LightViewBufferFar";

            LightProjectionViewSetNear = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory), LightProjectionBufferNear, LightViewBufferNear));
            LightProjectionViewSetMid = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory), LightProjectionBufferMid, LightViewBufferMid));
            LightProjectionViewSetFar = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory), LightProjectionBufferFar, LightViewBufferFar));

            ShadowMaps = new CascadedShadowMaps(graphicsDevice);

            TextureSamplerResourceLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            CascadeInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var shadowLayout = ResourceLayoutFactory.GetShadowLayout(resourceFactory);
            ShadowResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(shadowLayout, 
                LightProjectionBufferNear, LightViewBufferNear,
                LightProjectionBufferMid, LightViewBufferMid,
                LightProjectionBufferFar, LightViewBufferFar, CascadeInfoBuffer,
                NearShadowMapView, MidShadowMapView, FarShadowMapView, graphicsDevice.PointSampler));

            DrawDeltaBuffer = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            DrawDeltaComputeSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayoutFactory.GetDrawDeltaComputeLayout(resourceFactory), DrawDeltaBuffer));

            this.mainSceneSampleCount = mainSceneSampleCount;

            RecreateWindowSizedResources(graphicsDevice, resourceFactory);
        }

        public void RecreateWindowSizedResources(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            MainSceneColorTexture?.Dispose();
            MainSceneDepthTexture?.Dispose();
            MainSceneResolvedColorTexture?.Dispose();
            MainSceneResolvedColorView?.Dispose();
            MainSceneViewResourceSet?.Dispose();
            MainSceneFramebuffer?.Dispose();
            DuplicatorTarget0?.Dispose();
            DuplicatorTarget1?.Dispose();
            DuplicatorTargetView0?.Dispose();
            DuplicatorTargetView1?.Dispose();
            DuplicatorTargetSet0?.Dispose();
            DuplicatorTargetSet1?.Dispose();
            DuplicatorFramebuffer?.Dispose();

            var mainColorDesc = TextureDescription.Texture2D(
                graphicsDevice.SwapchainFramebuffer.Width,
                graphicsDevice.SwapchainFramebuffer.Height,
                1,
                1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                mainSceneSampleCount);

            MainSceneColorTexture = resourceFactory.CreateTexture(ref mainColorDesc);
            if (mainSceneSampleCount != TextureSampleCount.Count1)
            {
                mainColorDesc.SampleCount = TextureSampleCount.Count1;
                MainSceneResolvedColorTexture = resourceFactory.CreateTexture(ref mainColorDesc);
            }
            else
            {
                MainSceneResolvedColorTexture = MainSceneColorTexture;
            }
            MainSceneResolvedColorView = resourceFactory.CreateTextureView(MainSceneResolvedColorTexture);

            MainSceneDepthTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                graphicsDevice.SwapchainFramebuffer.Width,
                graphicsDevice.SwapchainFramebuffer.Height,
                1,
                1,
                PixelFormat.R32_Float,
                TextureUsage.DepthStencil,
                mainSceneSampleCount));

            MainSceneFramebuffer = resourceFactory.CreateFramebuffer(new FramebufferDescription(MainSceneDepthTexture, MainSceneColorTexture));
            MainSceneViewResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(TextureSamplerResourceLayout, MainSceneResolvedColorView, graphicsDevice.PointSampler));

            var colorTargetDesc = TextureDescription.Texture2D(
                graphicsDevice.SwapchainFramebuffer.Width,
                graphicsDevice.SwapchainFramebuffer.Height,
                1,
                1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled);
            DuplicatorTarget0 = resourceFactory.CreateTexture(ref colorTargetDesc);
            DuplicatorTargetView0 = resourceFactory.CreateTextureView(DuplicatorTarget0);
            DuplicatorTarget1 = resourceFactory.CreateTexture(ref colorTargetDesc);
            DuplicatorTargetView1 = resourceFactory.CreateTextureView(DuplicatorTarget1);
            DuplicatorTargetSet0 = resourceFactory.CreateResourceSet(new ResourceSetDescription(TextureSamplerResourceLayout, DuplicatorTargetView0, graphicsDevice.PointSampler));
            DuplicatorTargetSet1 = resourceFactory.CreateResourceSet(new ResourceSetDescription(TextureSamplerResourceLayout, DuplicatorTargetView1, graphicsDevice.PointSampler));

            var fbDesc = new FramebufferDescription(null, DuplicatorTarget0, DuplicatorTarget1);
            DuplicatorFramebuffer = resourceFactory.CreateFramebuffer(ref fbDesc);
        }

        public void Dispose()
        {
            DrawDeltaBuffer?.Dispose();
            DrawDeltaBuffer = null;
            DrawDeltaComputeSet?.Dispose();
            DrawDeltaComputeSet = null;
            TextureSamplerResourceLayout?.Dispose();
            MainSceneColorTexture?.Dispose();
            MainSceneDepthTexture?.Dispose();
            MainSceneResolvedColorTexture?.Dispose();
            MainSceneResolvedColorView?.Dispose();
            MainSceneViewResourceSet?.Dispose();
            MainSceneFramebuffer?.Dispose();
            DuplicatorTarget0?.Dispose();
            DuplicatorTarget1?.Dispose();
            DuplicatorTargetView0?.Dispose();
            DuplicatorTargetView1?.Dispose();
            DuplicatorTargetSet0?.Dispose();
            DuplicatorTargetSet1?.Dispose();
            DuplicatorFramebuffer?.Dispose();
            ShadowMaps.DestroyDeviceObjects();
            LightProjectionBufferNear.Dispose();
            LightProjectionBufferMid.Dispose();
            LightProjectionBufferFar.Dispose();
            LightProjectionViewSetNear.Dispose();
            LightProjectionViewSetMid.Dispose();
            LightProjectionViewSetFar.Dispose();
            LightViewBufferNear.Dispose();
            LightViewBufferMid.Dispose();
            LightViewBufferFar.Dispose();
            CascadeInfoBuffer.Dispose();
            ShadowResourceSet?.Dispose();
        }
    }
}

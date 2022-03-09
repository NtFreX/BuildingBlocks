using Veldrid;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Light
{
    public class CascadedShadowMaps
    {
        //TODO: stabelize
        public const float LScale = 1f;
        public const float RScale = 1f;
        public const float TScale = 1f;
        public const float BScale = 1f;
        public const float NScale = 4f;
        public const float FScale = 4f;
        public const float NearCascadeLimit = 100;
        public const float MidCascadeLimit = 300;
        public const float FarCascadeLimit = 900;

        public VeldridTexture NearShadowMap { get; private set; }
        public TextureView NearShadowMapView { get; private set; }
        public Framebuffer NearShadowMapFramebuffer { get; private set; }

        public VeldridTexture MidShadowMap { get; private set; }
        public TextureView MidShadowMapView { get; private set; }
        public Framebuffer MidShadowMapFramebuffer { get; private set; }

        public VeldridTexture FarShadowMap { get; private set; }
        public TextureView FarShadowMapView { get; private set; }
        public Framebuffer FarShadowMapFramebuffer { get; private set; }

        //TODO: support more cascades
        public CascadedShadowMaps(GraphicsDevice gd, float shadowDetail = 2f)
        {
            var factory = gd.ResourceFactory;
            TextureDescription desc = TextureDescription.Texture2D((uint)(2048 * shadowDetail), (uint)(2048 * shadowDetail), 1, 1, PixelFormat.D32_Float_S8_UInt, TextureUsage.DepthStencil | TextureUsage.Sampled);
            NearShadowMap = factory.CreateTexture(desc);
            NearShadowMap.Name = "Near Shadow Map";
            NearShadowMapView = factory.CreateTextureView(NearShadowMap);
            NearShadowMapFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(
                new FramebufferAttachmentDescription(NearShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));

            MidShadowMap = factory.CreateTexture(desc);
            MidShadowMapView = factory.CreateTextureView(new TextureViewDescription(MidShadowMap, 0, 1, 0, 1));
            MidShadowMapFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(
                new FramebufferAttachmentDescription(MidShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));

            FarShadowMap = factory.CreateTexture(desc);
            FarShadowMapView = factory.CreateTextureView(new TextureViewDescription(FarShadowMap, 0, 1, 0, 1));
            FarShadowMapFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(
                new FramebufferAttachmentDescription(FarShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));
        }

        public void DestroyDeviceObjects()
        {
            NearShadowMapFramebuffer.Dispose();
            NearShadowMapView.Dispose();
            NearShadowMap.Dispose();

            MidShadowMapFramebuffer.Dispose();
            MidShadowMapView.Dispose();
            MidShadowMap.Dispose();

            FarShadowMapFramebuffer.Dispose();
            FarShadowMapView.Dispose();
            FarShadowMap.Dispose();
        }
    }
}

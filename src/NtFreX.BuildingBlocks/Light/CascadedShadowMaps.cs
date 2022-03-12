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

        public readonly float NearCascadeLimit = 100;
        public readonly float MidCascadeLimit = 300;
        public readonly float FarCascadeLimit = 900;

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
        public CascadedShadowMaps(GraphicsDevice gd, ResourceFactory resourceFactory, uint shadowMapResolution = 2048 * 2048)
        {
            var size = (uint) Math.Sqrt(shadowMapResolution);
            TextureDescription desc = TextureDescription.Texture2D(size, size, 1, 1, PixelFormat.D32_Float_S8_UInt, TextureUsage.DepthStencil | TextureUsage.Sampled);
            NearShadowMap = resourceFactory.CreateTexture(desc);
            NearShadowMap.Name = "Near Shadow Map Texture";
            NearShadowMapView = resourceFactory.CreateTextureView(NearShadowMap);
            NearShadowMapView.Name = "Near Shadow Map View";
            NearShadowMapFramebuffer = resourceFactory.CreateFramebuffer(new FramebufferDescription(new FramebufferAttachmentDescription(NearShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));
            NearShadowMapFramebuffer.Name = "Near Shadow Map Framebuffer";

            MidShadowMap = resourceFactory.CreateTexture(desc);
            MidShadowMap.Name = "Mid Shadow Map Texture";
            MidShadowMapView = resourceFactory.CreateTextureView(new TextureViewDescription(MidShadowMap, 0, 1, 0, 1));
            MidShadowMapView.Name = "Mid Shadow Map View";
            MidShadowMapFramebuffer = resourceFactory.CreateFramebuffer(new FramebufferDescription(new FramebufferAttachmentDescription(MidShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));
            MidShadowMapFramebuffer.Name = "Mid Shadow Map Framebuffer";

            FarShadowMap = resourceFactory.CreateTexture(desc);
            FarShadowMap.Name = "Far Shadow Map Texture";
            FarShadowMapView = resourceFactory.CreateTextureView(new TextureViewDescription(FarShadowMap, 0, 1, 0, 1));
            FarShadowMapView.Name = "Far Shadow Map View";
            FarShadowMapFramebuffer = resourceFactory.CreateFramebuffer(new FramebufferDescription(new FramebufferAttachmentDescription(FarShadowMap, 0), Array.Empty<FramebufferAttachmentDescription>()));
            FarShadowMapFramebuffer.Name = "Far Shadow Map Framebuffer";
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

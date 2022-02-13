using Veldrid;

namespace NtFreX.BuildingBlocks.Model
{
    public class RenderContext
    {
        public Framebuffer MainSceneFramebuffer { get; private set; }

        public RenderContext(Framebuffer mainSceneFramebuffer)
        {
            MainSceneFramebuffer = mainSceneFramebuffer;
        }
    }
}

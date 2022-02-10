using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh
{
    public interface IRenderable
    {
        RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        void Draw(CommandList commandList);
    }
}

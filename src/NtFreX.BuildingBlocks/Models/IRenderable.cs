using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Models
{
    public interface IRenderable
    {
        RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        void Draw(CommandList commandList);
    }
}

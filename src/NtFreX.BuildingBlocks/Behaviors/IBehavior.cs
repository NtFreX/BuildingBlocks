using NtFreX.BuildingBlocks.Models;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public interface IBehavior
    {
        void Update(GraphicsSystem graphicsSystem, Model model, float delta);
    }
}

using NtFreX.BuildingBlocks.Input;

namespace NtFreX.BuildingBlocks.Model
{
    public interface IUpdateable
    {
        void Update(float deltaSeconds, InputHandler inputHandler);
    }
}

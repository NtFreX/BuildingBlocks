using NtFreX.BuildingBlocks.Models;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class AlwaysFaceCameraBehavior : IBehavior
    {
        public void Update(GraphicsSystem graphicsSystem, Model model, float delta)
        {
            // TODO rotate arround center
            model.Rotation.Value = Quaternion.Inverse(Quaternion.CreateFromRotationMatrix(graphicsSystem.Camera.ViewMatrix * Matrix4x4.CreateRotationX((float)-(Math.PI / 180f * 90f))));
        }
    }
}

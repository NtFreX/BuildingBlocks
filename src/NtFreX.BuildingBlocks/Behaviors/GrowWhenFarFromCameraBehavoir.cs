using NtFreX.BuildingBlocks.Models;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class GrowWhenFarFromCameraBehavoir : IBehavior
    {
        private readonly float growFactor;
        private Vector3? firstScale;

        public GrowWhenFarFromCameraBehavoir(float growFactor)
        {
            this.growFactor = growFactor;
        }

        public void Update(GraphicsSystem graphicsSystem, Model model, float delta)
        {
            if (firstScale == null)
                firstScale = model.Scale.Value;

            model.Scale.Value = firstScale.Value * Vector3.Distance(model.Position.Value, graphicsSystem.Camera.Position) * growFactor;
        }
    }
}

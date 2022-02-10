using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class GrowWhenFarFromCameraBehavoir : IBehavior
    {
        private readonly Model model;
        private readonly float growFactor;
        private Vector3? firstScale;

        public GrowWhenFarFromCameraBehavoir(Model model, float growFactor)
        {
            this.model = model;
            this.growFactor = growFactor;
        }

        public void Dispose() { }

        public void Update(float delta)
        {
            if (model.GraphicsSystem.Camera.Value == null)
                return;

            if (firstScale == null)
                firstScale = model.Scale.Value;

            model.Scale.Value = firstScale.Value * Vector3.Distance(model.Position.Value, model.GraphicsSystem.Camera.Value.Position) * growFactor;
        }
    }
}

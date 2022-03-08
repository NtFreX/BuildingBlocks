using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Model.Behaviors
{
    public class GrowWhenFarFromCameraBehavoir : IUpdateable
    {
        private readonly MeshRenderer model;
        private readonly float growFactor;
        private Vector3? firstScale;

        public GrowWhenFarFromCameraBehavoir(MeshRenderer model, float growFactor)
        {
            this.model = model;
            this.growFactor = growFactor;
        }

        public void Update(float delta, InputHandler inputHandler)
        {
            if (model.CurrentScene?.Camera.Value == null)
                return;

            if (firstScale == null)
                firstScale = model.Transform.Value.Scale;

            model.Transform.Value = model.Transform.Value with { Scale = firstScale.Value * Vector3.Distance(model.Transform.Value.Position, model.CurrentScene.Camera.Value.Position) * growFactor };
        }
    }
}

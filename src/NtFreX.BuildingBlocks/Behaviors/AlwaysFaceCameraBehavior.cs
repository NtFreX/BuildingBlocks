using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class AlwaysFaceCameraBehavior : IBehavior
    {
        private readonly Model model;
        private readonly Matrix4x4? rotationOffset;

        public void Dispose() { }

        public AlwaysFaceCameraBehavior(Model model, Matrix4x4? rotationOffset = null)
        {
            this.model = model;
            this.rotationOffset = rotationOffset;
        }

        public void Update(float delta)
        {
            if (model.GraphicsSystem.Camera.Value == null)
                return;

            // TODO rotate arround cente
            model.Rotation.Value = Quaternion.Inverse(Quaternion.CreateFromRotationMatrix(model.GraphicsSystem.Camera.Value.ViewMatrix * (rotationOffset ?? Matrix4x4.Identity) * Matrix4x4.CreateRotationX((float)-(Math.PI / 180f * 90f))));
        }
    }
}

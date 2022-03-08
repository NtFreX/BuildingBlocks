using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Model.Behaviors
{
    public class AlwaysFaceCameraBehavior : IUpdateable
    {
        private readonly MeshRenderer model;
        private readonly Matrix4x4? rotationOffset;

        public AlwaysFaceCameraBehavior(MeshRenderer model, Matrix4x4? rotationOffset = null)
        {
            this.model = model;
            this.rotationOffset = rotationOffset;
        }

        public void Update(float delta, InputHandler inputHandler)
        {
            if (model.CurrentScene?.Camera.Value == null)
                return;

            // TODO rotate arround cente
            // TODO: do not move from matrix to quaternion and back
            var rotation = Quaternion.Inverse(Quaternion.CreateFromRotationMatrix(model.CurrentScene.Camera.Value.ViewMatrix * (rotationOffset ?? Matrix4x4.Identity) * Matrix4x4.CreateRotationX((float)-(Math.PI / 180f * 90f))));
            model.Transform.Value = model.Transform.Value with { Rotation = Matrix4x4.CreateFromQuaternion(rotation) };
        }
    }
}

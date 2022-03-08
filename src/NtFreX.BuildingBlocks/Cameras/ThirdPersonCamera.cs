using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Cameras
{
    public class ThirdPersonCamera : Camera
    {
        public MeshRenderer? Model { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Offset { get; set; }
        public Vector3 LookAtOffset { get; set; }

        public ThirdPersonCamera(float windowWidth, float windowHeight, Vector3 forward, Vector3? offset = null, Vector3? lookAtOffset = null)
           : base(windowWidth, windowHeight)
        {
            Forward = forward;
            Offset = offset ?? Vector3.Zero;
            LookAtOffset = lookAtOffset ?? Vector3.Zero;
        }

        public override void AfterModelUpdate(InputHandler inputs, float deltaSeconds)
        {
            base.AfterModelUpdate(inputs, deltaSeconds);

            if (Model == null)
                return;

            Position.Value = Model.Transform.Value.Position + Vector3.Transform(Offset, Model.Transform.Value.Rotation);
            LookAt.Value = Position.Value + Vector3.Transform(Forward + LookAtOffset, Model.Transform.Value.Rotation);
        }
    }
}

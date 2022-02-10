using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Cameras
{
    public class ThirdPersonCamera : Camera
    {
        public Model? Model { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Offset { get; set; }
        public Vector3 LookAtOffset { get; set; }

        public ThirdPersonCamera(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, float windowWidth, float windowHeight, Vector3 forward, Vector3? offset = null, Vector3? lookAtOffset = null)
           : base(graphicsDevice, resourceFactory, windowWidth, windowHeight)
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

            Position.Value = Model.Position.Value + Vector3.Transform(Offset, Model.Rotation.Value);
            LookAt.Value = Position.Value + Vector3.Transform(Forward + LookAtOffset, Model.Rotation.Value);
        }
    }
}

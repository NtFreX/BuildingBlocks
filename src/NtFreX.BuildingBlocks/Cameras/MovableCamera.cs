using NtFreX.BuildingBlocks.Input;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Cameras
{
    public class MovableCamera : Camera
    {
        private float yawn = 0f;
        private float pitch = 0f;
        private float speed = 10f;
        private float fastSpeed = 100f;

        private Vector2? previousMousePos = null;

        public MovableCamera(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, float windowWidth, float windowHeight) 
            : base(graphicsDevice, resourceFactory, windowWidth, windowHeight) { }

        public override void BeforeModelUpdate(InputHandler inputs, float deltaSeconds)
        {
            base.BeforeModelUpdate(inputs, deltaSeconds);

            Vector3 motionDir = Vector3.Zero;
            if (inputs.IsKeyDown(Key.A))
            {
                motionDir += -Vector3.UnitX;
            }
            if (inputs.IsKeyDown(Key.D))
            {
                motionDir += Vector3.UnitX;
            }
            if (inputs.IsKeyDown(Key.W))
            {
                motionDir += -Vector3.UnitZ;
            }
            if (inputs.IsKeyDown(Key.S))
            {
                motionDir += Vector3.UnitZ;
            }
            if (inputs.IsKeyDown(Key.Q))
            {
                motionDir += -Vector3.UnitY;
            }
            if (inputs.IsKeyDown(Key.E))
            {
                motionDir += Vector3.UnitY;
            }

            if (motionDir != Vector3.Zero)
            {
                var realSpeed = inputs.IsKeyDown(Key.ShiftLeft) ? fastSpeed : speed;
                var lookRotation = Quaternion.CreateFromYawPitchRoll(yawn, pitch, 0f);
                motionDir = Vector3.Transform(motionDir, lookRotation);
                var motion = motionDir * deltaSeconds * realSpeed;
                Position.Value += motion;
                SetLookAt();
            }

            if (previousMousePos != null && (inputs.IsMouseDown(MouseButton.Left) || inputs.IsMouseDown(MouseButton.Right)))
            {
                Vector2 mouseDelta = inputs.CurrentSnapshot.MousePosition - previousMousePos.Value;
                yawn += -mouseDelta.X * 0.01f;
                pitch += -mouseDelta.Y * 0.01f;
                SetLookAt();
            }
            previousMousePos = inputs.CurrentSnapshot.MousePosition;
        }
        
        private void SetLookAt()
        {
            var lookRotation = Quaternion.CreateFromYawPitchRoll(yawn, pitch, 0f);
            var lookDir = Vector3.Transform(-Vector3.UnitZ, lookRotation);
            LookAt.Value = Position + lookDir;
        }
    }
}

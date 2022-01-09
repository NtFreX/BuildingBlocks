using NtFreX.BuildingBlocks.Input;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Cameras
{
    public class MapCamera : Camera
    {
        private float moveSpeed = 15f;
        private float rotationSpeed = 2f;
        private float pitchSpeed = 5f;
        private float zoomSpeed = 10f;

        private float pitch = -0.2f;
        private float rotation = 0f;
        private float distance = 10f;

        public MapCamera(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, float windowWidth, float windowHeight)
            : base(graphicsDevice, resourceFactory, windowWidth, windowHeight) { }

        public override void Update(GraphicsDevice graphicsDevice, InputHandler inputs, float deltaSeconds)
        {
            base.Update(graphicsDevice, inputs, deltaSeconds);

            Vector3 direction = Vector3.Zero;
            float pitchDir = 0f;  
            float rotationDir = 0f;
            float zoomDir = 0f;
            if (inputs.IsKeyDown(Key.A))
            {
                direction = Vector3.UnitX;
            }
            if (inputs.IsKeyDown(Key.D))
            {
                direction = -Vector3.UnitX;
            }
            if (inputs.IsKeyDown(Key.W))
            {
                direction = Vector3.UnitZ;
            }
            if (inputs.IsKeyDown(Key.S))
            {
                direction = -Vector3.UnitZ;
            }

            if (inputs.IsKeyDown(Key.X))
            {
                pitchDir = -1f;
            }
            if (inputs.IsKeyDown(Key.Z))
            {
                pitchDir = 1f;
            }

            if (inputs.IsKeyDown(Key.Q))
            {
                rotationDir = -1f;
            }
            if (inputs.IsKeyDown(Key.E))
            {
                rotationDir = 1f;
            }

            if (inputs.IsKeyDown(Key.KeypadPlus))
            {
                zoomDir = -1f;
            }
            if (inputs.IsKeyDown(Key.KeypadMinus))
            {
                zoomDir = 1f;
            }

            if(zoomDir != 0f)
            {
                distance += deltaSeconds * zoomDir * zoomSpeed;
                distance = distance < 2.5f ? 2.5f : distance;
                distance = distance > 50f ? 50f : distance;
            }

            if (direction != Vector3.Zero)
            {
                LookAt.Value += Vector3.Transform(direction * moveSpeed * deltaSeconds, Matrix4x4.CreateRotationY(-rotation));
            }

            if(pitchDir != 0f)
            {
                pitch += pitchDir * pitchSpeed * deltaSeconds;
                pitch = pitch > -0.2f ? -0.2f : pitch;
                pitch = pitch < -0.9f ? -0.9f : pitch;
            }

            if(rotationDir != 0f)
            {
                rotation += rotationDir * rotationSpeed * deltaSeconds;
            }

            Position.Value = LookAt + Vector3.Transform(Vector3.One, Matrix4x4.CreateTranslation(new Vector3(0f, 0f, -distance)) * Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateRotationY(-rotation));
        }
    }
}

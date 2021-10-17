using System.Numerics;

namespace NtFreX.BuildingBlocks.Desktop
{
    public class Camera
    {
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }

        public readonly Mutable<float> WindowWidth;
        public readonly Mutable<float> WindowHeight;

        public readonly Mutable<float> FieldOfView = new Mutable<float>(1f);
        public readonly Mutable<float> NearDistance = new Mutable<float>(0.1f);
        public readonly Mutable<float> FarDistance = new Mutable<float>(1000f);

        public readonly Mutable<Vector3> Up = new Mutable<Vector3>(Vector3.UnitY);
        public readonly Mutable<Vector3> Position = new Mutable<Vector3>(new Vector3(0f, 0.5f, 2f));
        public readonly Mutable<Vector3> LookAt = new Mutable<Vector3>(new Vector3(0, 0, 0));

        public Camera(float windowWidth, float windowHeight)
        {
            WindowWidth = new Mutable<float>(windowWidth);
            WindowHeight = new Mutable<float>(windowHeight);

            WindowWidth.ValueChanged += (_, _) => UpdateProjectionMatrix();
            WindowHeight.ValueChanged += (_, _) => UpdateProjectionMatrix();
            FieldOfView.ValueChanged += (_, _) => UpdateProjectionMatrix();
            NearDistance.ValueChanged += (_, _) => UpdateProjectionMatrix();
            FarDistance.ValueChanged += (_, _) => UpdateProjectionMatrix();

            Up.ValueChanged += (_, _) => UpdateViewMatrix();
            Position.ValueChanged += (_, _) => UpdateViewMatrix();
            LookAt.ValueChanged += (_, _) => UpdateViewMatrix();

            UpdateProjectionMatrix();
            UpdateViewMatrix();
        }

        private void UpdateProjectionMatrix()
            => ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView.Value, WindowWidth.Value / WindowHeight.Value, NearDistance.Value, FarDistance.Value);
        private void UpdateViewMatrix()
            => ViewMatrix = Matrix4x4.CreateLookAt(Position.Value, LookAt.Value, Up.Value);
    }
}

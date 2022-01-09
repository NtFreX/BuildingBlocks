using NtFreX.BuildingBlocks.Desktop;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Cameras
{
    public abstract class Camera
    {
        public DeviceBuffer CameraInfoBuffer { get; private set; }
        public DeviceBuffer ProjectionBuffer { get; private set; }
        public DeviceBuffer ViewBuffer { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }

        public readonly Mutable<float> WindowWidth;
        public readonly Mutable<float> WindowHeight;

        public readonly Mutable<float> FieldOfView = new Mutable<float>(1f);
        public readonly Mutable<float> NearDistance = new Mutable<float>(0.1f);
        public readonly Mutable<float> FarDistance = new Mutable<float>(10000f);

        public readonly Mutable<Vector3> Up = new Mutable<Vector3>(Vector3.UnitY);
        public readonly Mutable<Vector3> Position = new Mutable<Vector3>(new Vector3(0, .5f, 2f));
        public readonly Mutable<Vector3> LookAt = new Mutable<Vector3>(new Vector3(0, 0, 0));

        private bool hasProjectionChanged = true;
        private bool hasViewChanged = true;

        public unsafe Camera(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, float windowWidth, float windowHeight)
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

            ProjectionBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            ViewBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            CameraInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)sizeof(CameraInfo), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            UpdateProjectionMatrix();
            UpdateViewMatrix();
            Update(graphicsDevice, InputHandler.Empty, 0f);
        }

        private void UpdateProjectionMatrix()
        {
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, WindowWidth / WindowHeight, NearDistance, FarDistance);
            hasProjectionChanged = true;
        }
        private void UpdateViewMatrix()
        {
            ViewMatrix = Matrix4x4.CreateLookAt(Position, LookAt, Up);
            hasViewChanged = true;
        }

        public virtual void Update(GraphicsDevice graphicsDevice, InputHandler inputs, float deltaSeconds)
        {
            if(hasProjectionChanged || hasViewChanged)
            {
                var cameraInfo = new CameraInfo
                {
                    CameraFarPlaneDistance = FarDistance,
                    CameraNearPlaneDistance = NearDistance,
                    CameraPosition = Position,
                    CameraLookDirection = LookAt
                };
                graphicsDevice.UpdateBuffer(CameraInfoBuffer, 0, cameraInfo);
            }

            if (hasProjectionChanged)
            {
                graphicsDevice.UpdateBuffer(ProjectionBuffer, 0, ProjectionMatrix);
                hasProjectionChanged = false;
            }
            if (hasViewChanged)
            {
                graphicsDevice.UpdateBuffer(ViewBuffer, 0, ViewMatrix);
                hasViewChanged = false;
            }
        }
    }
}

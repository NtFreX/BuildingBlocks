using NtFreX.BuildingBlocks.Desktop;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Cameras
{
    public abstract class Camera
    {
        public ResourceSet? ProjectionViewResourceSet { get; private set; }
        public ResourceSet? CameraInfoResourceSet { get; private set; }
        public DeviceBuffer? CameraInfoBuffer { get; private set; }
        public DeviceBuffer? ProjectionBuffer { get; private set; }
        public DeviceBuffer? ViewBuffer { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }

        public readonly Mutable<float> WindowWidth;
        public readonly Mutable<float> WindowHeight;

        public readonly Mutable<float> FieldOfView;
        public readonly Mutable<float> NearDistance;
        public readonly Mutable<float> FarDistance;

        public readonly Mutable<Vector3> Up;
        public readonly Mutable<Vector3> Position;
        public readonly Mutable<Vector3> LookAt;
        public float AspectRatio { get; private set; }

        private bool hasProjectionChanged = true;
        private bool hasViewChanged = true;

        private GraphicsDevice? graphicsDevice;

        public Camera(float windowWidth, float windowHeight)
        {
            FieldOfView = new Mutable<float>(1f, this); 
            NearDistance = new Mutable<float>(0.1f, this);
            FarDistance = new Mutable<float>(10000f, this);
            Up = new Mutable<Vector3>(Vector3.UnitY, this);
            Position = new Mutable<Vector3>(new Vector3(0, .5f, 2f), this);
            LookAt = new Mutable<Vector3>(new Vector3(0, 0, 0), this);
            WindowWidth = new Mutable<float>(windowWidth, this);
            WindowHeight = new Mutable<float>(windowHeight, this);

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
            BeforeModelUpdate(InputHandler.Empty, 0f);
            AfterModelUpdate(InputHandler.Empty, 0f);
        }

        public unsafe void CreateDeviceResources(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(this.graphicsDevice == null);

            this.graphicsDevice = graphicsDevice;

            ProjectionBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            ViewBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            CameraInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)sizeof(CameraInfo), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var cameraInfoLayout = ResourceLayoutFactory.GetCameraInfoFragmentLayout(resourceFactory);
            CameraInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(cameraInfoLayout, CameraInfoBuffer));

            var projectionViewLayout = ResourceLayoutFactory.GetProjectionViewLayout(resourceFactory);
            ProjectionViewResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(projectionViewLayout, ProjectionBuffer, ViewBuffer));
        }

        public void DestroyDeviceResources()
        {
            graphicsDevice = null;

            ProjectionBuffer?.Dispose();
            ProjectionBuffer = null;

            ViewBuffer?.Dispose();
            ViewBuffer = null;

            CameraInfoBuffer?.Dispose();
            CameraInfoBuffer = null;

            CameraInfoResourceSet?.Dispose();
            CameraInfoResourceSet = null;

            ProjectionViewResourceSet?.Dispose();
            ProjectionViewResourceSet = null;
        }

        private void UpdateProjectionMatrix()
        {
            // The projection is only lazy updated to make sure a graphic device is present
            hasProjectionChanged = true;
        }
        private void UpdateViewMatrix()
        {
            ViewMatrix = Matrix4x4.CreateLookAt(Position, LookAt, Up);
            hasViewChanged = true;
        }
        public virtual void BeforeModelUpdate(InputHandler inputs, float deltaSeconds)
        {
            if (graphicsDevice == null)
                return;

            //TODO: analyise if updating directly is smarter (use initial graphics device, what happens when it changes?) then doing it lazy here once we have a valid graphics device
            if (hasProjectionChanged || hasViewChanged)
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
                AspectRatio = WindowWidth / WindowHeight;
                ProjectionMatrix = Matrix4x4Extensions.CreatePerspective(graphicsDevice.IsClipSpaceYInverted, graphicsDevice.IsDepthRangeZeroToOne, FieldOfView, AspectRatio, NearDistance, FarDistance);
                graphicsDevice.UpdateBuffer(ProjectionBuffer, 0, ProjectionMatrix);
                hasProjectionChanged = false;
            }
            if (hasViewChanged)
            {
                graphicsDevice.UpdateBuffer(ViewBuffer, 0, ViewMatrix);
                hasViewChanged = false;
            }
        }
        public virtual void AfterModelUpdate(InputHandler inputs, float deltaSeconds)
        { }
    }
}

using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Models;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class CollidableBehavoir<TShape> : IBehavior
        where TShape : unmanaged, IShape
    {
        private readonly Collider<TShape> collider;
        private readonly Model model;
        private int? isUpdatingThread;

        public CollidableBehavoir(Simulation simulation, Model model, bool dynamic = false, float mass = 1f, TShape? shape = null, BodyVelocity? velocity = null)
        {
            var creationInfo = new ModelCreationInfo { Position = model.Position.Value, Rotation = model.Rotation.Value, Scale = model.Scale.Value };
            var objectShape = shape ?? ((PhysicsMeshDeviceBuffer<TShape>)model.MeshBuffer).ShapeAllocator.Invoke(simulation);
            this.collider = new Collider<TShape>(model.MeshBuffer.PrimitiveTopology, objectShape, simulation, creationInfo, dynamic, mass, velocity);
            model.Position.ValueChanged += (_, _) => OnPoseChanged();
            model.Rotation.ValueChanged += (_, _) => OnPoseChanged();
            this.model = model;
        }

        private void OnPoseChanged()
        {
            if (Thread.CurrentThread.ManagedThreadId == isUpdatingThread)
                return;
            this.collider.SetPose(model.Position.Value, model.Rotation.Value);
        }

        public void Update(GraphicsSystem graphicsSystem, Model model, float delta)
        {
            isUpdatingThread = Thread.CurrentThread.ManagedThreadId;
            if (collider.IsDynamic)
            {
                var pose = this.collider.GetPose();
                model.Position.Value = pose.Position;
                model.Rotation.Value = pose.Orientation;
            }
            isUpdatingThread = null;
        }
    }
}

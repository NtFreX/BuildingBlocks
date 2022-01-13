using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Behaviors
{
    public class Collider<TShape>
        where TShape : unmanaged, IShape
    {
        public bool IsDynamic { get; private set; }

        private readonly BodyHandle? bodyHandle;
        private readonly StaticHandle? staticHandle;
        private readonly Simulation simulation;

        public unsafe Collider(PrimitiveTopology primitiveTopology, TShape shape, Simulation simulation, ModelCreationInfo creationInfo = default, bool dynamic = false, float mass = 1f, BodyVelocity? velocity = null)
        {
            if (primitiveTopology != PrimitiveTopology.TriangleList)
                throw new ArgumentException("Only triangle lists are supported to be colidable");

            IsDynamic = dynamic;

            var modelIndex = simulation.Shapes.Add(shape);

            var pose = new RigidPose(creationInfo.Position, creationInfo.Rotation);
            var collidable = new CollidableDescription(modelIndex);
            if (dynamic)
            {
                var inertia = new BodyInertia { InverseMass = mass };
                var actualVelocity = velocity ?? new BodyVelocity(linear: Vector3.Zero, angular: Vector3.Zero);
                var activity = new BodyActivityDescription(sleepThreshold: 0.01f);
                bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, actualVelocity, inertia, collidable, activity));
            }
            else
            {
                staticHandle = simulation.Statics.Add(new StaticDescription(pose.Position, pose.Orientation, collidable));
            }

            this.simulation = simulation;
        }

        public RigidPose GetPose()
        {
            if (bodyHandle != null)
            {
                var body = simulation.Bodies.GetBodyReference(bodyHandle.Value);
                return body.Pose;
            }
            if (staticHandle != null)
            {
                var body = simulation.Statics.GetStaticReference(staticHandle.Value);
                return body.Pose;
            }
            throw new Exception();
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            if (bodyHandle != null)
            {
                var body = simulation.Bodies.GetBodyReference(bodyHandle.Value);
                body.Pose = new RigidPose(position, rotation);
            }
            else if (staticHandle != null)
            {
                var body = simulation.Statics.GetStaticReference(staticHandle.Value);
                body.Pose = new RigidPose(position, rotation);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}

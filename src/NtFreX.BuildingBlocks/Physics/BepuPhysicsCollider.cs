using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Physics
{
    public sealed class BepuPhysicsCollider<TShape> : IDisposable
        where TShape : unmanaged, IShape
    {
        public BepuPhysicsBodyType BodyType { get; private set; }

        public readonly BodyHandle? BodyHandle;

        private readonly StaticHandle? staticHandle;
        private readonly Simulation simulation;

        public unsafe BepuPhysicsCollider(TShape shape, Simulation simulation, BodyInertia inertia, Transform transform = default, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, ContinuousDetectionMode continuousDetectionMode = ContinuousDetectionMode.Discrete, BodyVelocity? velocity = null)
        {
            BodyType = bodyType;

            var modelIndex = simulation.Shapes.Add(shape);

            var pose = new RigidPose(transform.Position, Quaternion.CreateFromRotationMatrix(transform.Rotation));
            var continuousDetection = new ContinuousDetection { Mode = continuousDetectionMode };
            var collidable = new CollidableDescription(modelIndex, continuousDetection);
            if (BodyType == BepuPhysicsBodyType.Dynamic)
            {
                var actualVelocity = velocity ?? new BodyVelocity(linear: Vector3.Zero, angular: Vector3.Zero);
                var activity = new BodyActivityDescription(BepuPhysicDefaults.DefaultBodySleepThreshold);
                BodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, actualVelocity, inertia, collidable, activity));
            }
            else if(BodyType == BepuPhysicsBodyType.Static)
            {
                staticHandle = simulation.Statics.Add(new StaticDescription(pose, modelIndex, continuousDetection));
            }
            
            this.simulation = simulation;
        }

        public BodyReference GetBodyReference() 
            => simulation.Bodies.GetBodyReference(BodyHandle ?? throw new NotSupportedException());

        public RigidPose GetPose()
        {
            if (BodyHandle != null)
            {
                var body = simulation.Bodies.GetBodyReference(BodyHandle.Value);
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
            if (BodyHandle != null)
            {
                var body = simulation.Bodies.GetBodyReference(BodyHandle.Value);
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

        public BodyVelocity GetVelocity()
        {
            if (BodyHandle != null)
            {
                var body = simulation.Bodies.GetBodyReference(BodyHandle.Value);
                return body.Velocity;
            }
            return new BodyVelocity();
        }

        public void Dispose()
        {
            if(BodyHandle != null)
            {
                simulation.Bodies.Remove(BodyHandle.Value);
            }
            if (staticHandle != null)
            {
                simulation.Statics.Remove(staticHandle.Value);
            }
        }
    }
}

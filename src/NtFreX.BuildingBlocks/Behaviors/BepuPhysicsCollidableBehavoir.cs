using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Physics;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Behaviors
{
    //TODO: support instances
    //TODO: can pause physics of model
    //TODO: only rigid body no collider
    //TODO: collider options (groups, just detection), default possibility but possibility to replace complete physics setup with custom code
    public class BepuPhysicsCollidableBehavoir<TShape> : IBehavior
        where TShape : unmanaged, IShape
    {
        private readonly Simulation simulation;
        private readonly Model[] models;
        private readonly BepuPhysicsBodyType bodyType;
        private readonly BodyInertia inertia;

        public BepuPhysicsCollider<TShape> Collider { get; private set; }

        public BepuPhysicsCollidableBehavoir(Simulation simulation, Model model, float mass = 1f, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, new[] { model }, mass, bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, Model[] models, float mass = 1f, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, models, CreateInertia(mass), bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, Model model, BodyInertia inertia, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, new [] { model }, inertia, bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, Model[] models, BodyInertia inertia, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
        {
            this.simulation = simulation;
            this.models = models;
            this.bodyType = bodyType;
            this.inertia = inertia;

            if (shape != null)
            {
                this.Collider = LoadCollider(shape.Value, velocity);
            }
            else if (models.Length == 1 && models.First().MeshBuffer is PhysicsMeshDeviceBuffer<TShape> physicsMeshBuffer)
            {
                this.Collider = LoadCollider(shape ?? physicsMeshBuffer.Shape, velocity);
                physicsMeshBuffer.Shape.ValueChanged += (sender, args) => ReloadCollider(args);
            }
            else
            {
                throw new ArgumentException("A shape must be provided or the given model array can only contain one item whose mesh buffer must be a PhysicsMeshDeviceBuffer");
            }

            foreach (var model in models)
            {
                model.Position.ValueChanged += (sender, args) => OnModelPoseChanged(args, model.Rotation.Value);
                model.Rotation.ValueChanged += (sender, args) => OnModelPoseChanged(model.Position.Value, args);
            }
        }

        private static BodyInertia CreateInertia(float mass)
        {
            var inverseMass = 1f / mass;
            return new BodyInertia
            {
                InverseMass = inverseMass,
                InverseInertiaTensor = new BepuUtilities.Symmetric3x3 { XX = inverseMass / 3f, YY = inverseMass / 3f, ZZ = inverseMass / 3f }
            };
        }

        private void ReloadCollider(TShape shape)
        {
            this.Collider?.Dispose();
            this.Collider = LoadCollider(shape);
        }

        private BepuPhysicsCollider<TShape> LoadCollider(TShape shape, BodyVelocity? velocity = null)
        {
            var creationInfo = new ModelCreationInfo { Position = models.First().Position.Value, Rotation = models.First().Rotation.Value, Scale = models.First().Scale.Value };
            return new BepuPhysicsCollider<TShape>(shape, simulation, inertia, creationInfo, bodyType,
                velocity: velocity ?? (this.Collider != null ? this.Collider.GetVelocity() : new BodyVelocity()));
        }

        private void OnModelPoseChanged(Vector3 position, Quaternion rotation)
        {
            var pose = this.Collider.GetPose();
            if(pose.Orientation != rotation || pose.Position != position)
                this.Collider.SetPose(position, rotation);
        }

        public void Update(float delta)
        {
            if (Collider.BodyType != BepuPhysicsBodyType.Static)
            {
                var pose = this.Collider.GetPose();
                foreach (var model in models)
                {
                    if (pose.Position != model.Position.Value)
                        model.Position.Value = pose.Position;
                    if (pose.Orientation != model.Rotation.Value)
                        model.Rotation.Value = pose.Orientation;
                }
            }
        }

        public void Dispose()
        {
            Collider.Dispose();
        }
    }
}

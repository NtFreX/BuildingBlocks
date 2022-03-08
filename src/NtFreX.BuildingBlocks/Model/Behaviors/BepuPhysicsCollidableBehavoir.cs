using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Physics;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Model.Behaviors
{
    //TODO: move to physics namespace? or move specialization from pysics namespace
    //TODO: support instances
    //TODO: can pause physics of model
    //TODO: only rigid body no collider
    //TODO: collider options (groups, just detection), default possibility but possibility to replace complete physics setup with custom code
    public class BepuPhysicsCollidableBehavoir<TShape> : IUpdateable
        where TShape : unmanaged, IShape
    {
        private readonly Simulation simulation;
        private readonly MeshRenderer[] models;
        private readonly BepuPhysicsBodyType bodyType;
        private readonly BodyInertia inertia;

        public BepuPhysicsCollider<TShape> Collider { get; private set; }

        public BepuPhysicsCollidableBehavoir(Simulation simulation, MeshRenderer model, float mass = 1f, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, new[] { model }, mass, bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, MeshRenderer[] models, float mass = 1f, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, models, CreateInertia(mass), bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, MeshRenderer model, BodyInertia inertia, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
            : this(simulation, new [] { model }, inertia, bodyType, velocity, shape) { }
        public BepuPhysicsCollidableBehavoir(Simulation simulation, MeshRenderer[] models, BodyInertia inertia, BepuPhysicsBodyType bodyType = BepuPhysicsBodyType.Static, BodyVelocity? velocity = null, TShape? shape = null)
        {
            this.simulation = simulation;
            this.models = models;
            this.bodyType = bodyType;
            this.inertia = inertia;

            if (shape != null)
            {
                this.Collider = LoadCollider(shape.Value, velocity);
            }
            else if (models.Length == 1 && models.First().MeshData.Specializations.TryGet<BepuPhysicsShapeMeshDataSpecialization<TShape>>(out var specialization))
            {
                this.Collider = LoadCollider(shape ?? specialization.Shape, velocity);
                // TODO: reload
                //models.First().MeshData.Specializations.SpecializationChanged += (_, _) => ReloadCollider(args);
                //specialization.Shape.ValueChanged += (sender, args) => ReloadCollider(args);
            }
            else
            {
                throw new ArgumentException("A shape must be provided or the given model array can only contain one item whose mesh buffer must be a PhysicsMeshDeviceBuffer");
            }

            foreach (var model in models)
            {
                model.Transform.ValueChanged += (sender, args) => OnModelPoseChanged(model.Transform.Value);
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
            return new BepuPhysicsCollider<TShape>(shape, simulation, inertia, models.First().Transform.Value, bodyType,
                velocity: velocity ?? (this.Collider != null ? this.Collider.GetVelocity() : new BodyVelocity()));
        }

        private void OnModelPoseChanged(Transform transform)
        {
            var pose = this.Collider.GetPose();
            var rotation = Quaternion.CreateFromRotationMatrix(transform.Rotation);
            if(pose.Orientation != rotation || pose.Position != transform.Position)
                this.Collider.SetPose(transform.Position, rotation);
        }

        public void Update(float delta, InputHandler inputHandler)
        {
            if (Collider.BodyType != BepuPhysicsBodyType.Static)
            {
                var pose = this.Collider.GetPose();
                foreach (var model in models)
                {
                    var rotation = Matrix4x4.CreateFromQuaternion(pose.Orientation);
                    if (pose.Position != model.Transform.Value.Position || rotation != model.Transform.Value.Rotation)
                    {
                        model.Transform.Value = model.Transform.Value with { Position = pose.Position, Rotation = rotation };
                    }
                }
            }
        }

        public void Dispose()
        {
            Collider.Dispose();
        }
    }
}

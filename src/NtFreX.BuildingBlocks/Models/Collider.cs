using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public class Collider
    {
        public bool IsDynamic { get; private set; }

        private Mesh mesh;

        private readonly BodyHandle? bodyHandle;
        private readonly StaticHandle? staticHandle;
        private readonly Simulation simulation;

        public unsafe Collider(PrimitiveTopology primitiveTopology, MeshData meshProvider, Simulation simulation, ModelCreationInfo creationInfo = default, bool dynamic = false, float mass = 1f)
        {
            if (primitiveTopology != PrimitiveTopology.TriangleList)
                throw new ArgumentException("Only triangle lists are supported to be colidable");

            IsDynamic = dynamic;

            var triangles = meshProvider.GetTriangles();
            simulation.BufferPool.Take<Triangle>(triangles.Length, out var buffer);
            
            mesh = new Mesh(buffer, creationInfo.Scale, simulation.BufferPool);
            var modelIndex = simulation.Shapes.Add(mesh);

            var pose = new RigidPose(creationInfo.Position, creationInfo.Rotation);
            if (dynamic)
            {
                var inertia = new BodyInertia { InverseMass = mass };
                bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, Vector3.Zero, inertia, modelIndex, 0.01f /* sleep threshold */));
            }
            else
            {
                staticHandle = simulation.Statics.Add(new StaticDescription(pose, modelIndex));
            }

            this.simulation = simulation;
        }

        public Vector3 Scale
        { 
            get => mesh.Scale;
            set => mesh.Scale = value; 
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

using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Standard;
using System.Buffers;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    //public class AutoRefreshPhysicsMeshDeviceBuffer<TShape> : PhysicsMeshDeviceBuffer<TShape>
    //    where TShape : unmanaged, IShape
    //{
    //    public Func<Simulation, Model, TShape> ShapeAllocator { get; }

    //    public AutoRefreshPhysicsMeshDeviceBuffer(
    //        PooledDeviceBuffer vertexBuffer, PooledDeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout,
    //        IndexFormat indexFormat, PrimitiveTopology primitiveTopology, TShape shape, Func<Simulation, Model, TShape> shapeAllocator, MaterialInfo? material = null, TextureView? textureView = null)
    //        : base(vertexBuffer, indexBuffer, indexLength, boundingBox, vertexLayout, indexFormat, primitiveTopology, shape, material, textureView)
    //    {
    //        ShapeAllocator = shapeAllocator;
    //    }

    //    public static PhysicsMeshDeviceBuffer<TShape> Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, TShape shape, TextureView? textureView = null, DeviceBufferPool? deviceBufferPool = null)
    //    {
    //        var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory, deviceBufferPool: deviceBufferPool);
    //        var boundingBox = mesh.GetBoundingBox();
    //        return new AutoRefreshPhysicsMeshDeviceBuffer<TShape>(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, boundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, shape, mesh.Material, textureView: textureView);
    //    }
    //}

    public class PhysicsMeshDeviceBuffer<TShape> : MeshDeviceBuffer
        where TShape : unmanaged, IShape
    {
        public Mutable<TShape> Shape { get; set; }
        //TODO: provide inertia

        public PhysicsMeshDeviceBuffer(MeshDeviceBuffer buffer, TShape shape)
            : base(buffer.VertexBuffer.Value, buffer.IndexBuffer.Value, buffer.IndexLength.Value, buffer.BoundingBox.Value, buffer.VertexLayout.Value, 
                  buffer.IndexFormat.Value, buffer.PrimitiveTopology.Value, buffer.MaterialInfoBuffer.Value, buffer.Material.Value, buffer.InstanceInfoBuffer.Value, 
                  buffer.Instances.Value.ToArray(), buffer.TextureView.Value)
        {
            Shape = new Mutable<TShape>(shape, this);
        }

        public static PhysicsMeshDeviceBuffer<TShape> Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, TShape shape, TextureView? textureView = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, BoundingBox? boundingBox = null)
        {
            var baseBuffer = Create(graphicsDevice, resourceFactory, mesh, textureView, deviceBufferPool, commandListPool, boundingBox);
            return new PhysicsMeshDeviceBuffer<TShape>(baseBuffer, shape);
        }
    }
    public class MeshDeviceBuffer : IDisposable
    {
        public Mutable<PolygonFillMode> FillMode { get; }
        public Mutable<FaceCullMode> CullMode { get; }
        public Mutable<PooledDeviceBuffer> MaterialInfoBuffer { get; }
        public Mutable<PooledDeviceBuffer> VertexBuffer { get; }
        public Mutable<PooledDeviceBuffer> IndexBuffer { get; }
        public Mutable<uint> IndexLength { get; }
        public Mutable<BoundingBox> BoundingBox { get; }
        public Mutable<BoundingBox> InstanceBoundingBox { get; }
        public Mutable<VertexLayoutDescription> VertexLayout { get; }
        public Mutable<IndexFormat> IndexFormat { get; }
        public Mutable<PrimitiveTopology> PrimitiveTopology { get; }
        public Mutable<MaterialInfo> Material { get; set; }
        public Mutable<TextureView?> TextureView { get; }
        public Mutable<IReadOnlyList<InstanceInfo>> Instances { get; }
        public Mutable<PooledDeviceBuffer> InstanceInfoBuffer { get; }

        public MeshDeviceBuffer(
            PooledDeviceBuffer vertexBuffer, PooledDeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout, 
            IndexFormat indexFormat, PrimitiveTopology primitiveTopology, PooledDeviceBuffer materialInfoBuffer, MaterialInfo material, PooledDeviceBuffer instanceInfoBuffer, InstanceInfo[] instances, TextureView? textureView = null)
        {
            VertexBuffer = new Mutable<PooledDeviceBuffer>(vertexBuffer, this);
            IndexBuffer = new Mutable<PooledDeviceBuffer>(indexBuffer, this);
            IndexLength = new Mutable<uint>(indexLength, this);
            BoundingBox = new Mutable<BoundingBox>(boundingBox, this);
            VertexLayout = new Mutable<VertexLayoutDescription>(vertexLayout, this);
            IndexFormat = new Mutable<IndexFormat>(indexFormat, this);
            PrimitiveTopology = new Mutable<PrimitiveTopology>(primitiveTopology, this);
            Material = new Mutable<MaterialInfo>(material, this);
            TextureView = new Mutable<TextureView?>(textureView, this);
            MaterialInfoBuffer = new Mutable<PooledDeviceBuffer>(materialInfoBuffer, this);
            Instances = new Mutable<IReadOnlyList<InstanceInfo>>(instances, this);
            InstanceInfoBuffer = new Mutable<PooledDeviceBuffer>(instanceInfoBuffer, this);
            InstanceBoundingBox = new Mutable<BoundingBox>(CalculateInstanceBoundingBox(), this);
            FillMode = new Mutable<PolygonFillMode>(PolygonFillMode.Solid, this);
            CullMode = new Mutable<FaceCullMode>(FaceCullMode.None, this);
        }

        public static MeshDeviceBuffer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, TextureView? textureView = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, BoundingBox? boundingBox = null)
        {
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory, deviceBufferPool, commandListPool);
            var realBoundingBox = boundingBox ?? mesh.GetBoundingBox();

            //TODO: make dynamic => reload bouding box when data changed, reload instance bounding box when instances changed etc

            // TODO: make buffers optional depending on layout

            var realInstances = mesh.Instances ?? InstanceInfo.Single;
            var materialBuffer = resourceFactory.GetMaterialBuffer(graphicsDevice, mesh.Material, deviceBufferPool);
            var instanceBuffer = resourceFactory.GetInstanceBuffer(graphicsDevice, realInstances, deviceBufferPool);

            var buffer = new MeshDeviceBuffer(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, realBoundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, materialBuffer, mesh.Material, instanceInfoBuffer: instanceBuffer, instances: realInstances, textureView: textureView);
            buffer.Material.ValueChanged += (_, _) => graphicsDevice.UpdateBuffer(buffer.MaterialInfoBuffer.Value.RealDeviceBuffer, 0, buffer.Material.Value);
            buffer.Instances.ValueChanged += (_, _) => graphicsDevice.UpdateBuffer(buffer.InstanceInfoBuffer.Value.RealDeviceBuffer, 0, buffer.Instances.Value.ToArray());
            buffer.BoundingBox.ValueChanged += (_, _) => buffer.InstanceBoundingBox.Value = buffer.CalculateInstanceBoundingBox();
            return buffer;
        }

        private BoundingBox CalculateInstanceBoundingBox()
        {
            BoundingBox? current = null;
            foreach(var instance in this.Instances.Value)
            {
                var worldMatrix = Transform.CreateWorldMatrix(instance.Position, Matrix4x4.CreateRotationX(instance.Rotation.X) * Matrix4x4.CreateRotationY(instance.Rotation.Y) * Matrix4x4.CreateRotationZ(instance.Rotation.Z), instance.Scale);
                var box = Veldrid.Utilities.BoundingBox.Transform(this.BoundingBox.Value, worldMatrix);
                current = current == null ? box : Veldrid.Utilities.BoundingBox.Combine(current.Value, box);
            }
            return current ?? throw new Exception("A mesh with no instance has no bounding box");
        }

        public void Set(MeshDeviceBuffer buffer) 
        {
            if(!object.ReferenceEquals(this.TextureView.Value, buffer.TextureView.Value))
            {
                this.TextureView.Value = buffer.TextureView.Value;
            }
            if (!this.Material.Value.Equals(buffer.Material.Value))
            {
                this.Material.Value = buffer.Material.Value;
            }
            if (this.PrimitiveTopology.Value != buffer.PrimitiveTopology.Value)
            {
                this.PrimitiveTopology.Value = buffer.PrimitiveTopology.Value;
            }
            if (this.IndexFormat.Value != buffer.IndexFormat.Value)
            {
                this.IndexFormat.Value = buffer.IndexFormat.Value;
            }
            if (!this.VertexLayout.Value.Equals(buffer.VertexLayout.Value))
            {
                this.VertexLayout.Value = buffer.VertexLayout.Value;
            }
            if (this.BoundingBox.Value != buffer.BoundingBox.Value)
            {
                this.BoundingBox.Value = buffer.BoundingBox.Value;
            }
            if (this.IndexLength.Value != buffer.IndexLength.Value)
            {
                this.IndexLength.Value = buffer.IndexLength.Value;
            }
            if (!object.ReferenceEquals(this.IndexBuffer.Value, buffer.IndexBuffer.Value))
            {
                this.IndexBuffer.Value.Free();
                this.IndexBuffer.Value = buffer.IndexBuffer.Value;
            }
            if (!object.ReferenceEquals(this.VertexBuffer.Value, buffer.VertexBuffer.Value))
            {
                this.VertexBuffer.Value.Free();
                this.VertexBuffer.Value = buffer.VertexBuffer.Value;
            }
            if (!object.ReferenceEquals(this.MaterialInfoBuffer.Value, buffer.MaterialInfoBuffer.Value))
            {
                this.MaterialInfoBuffer.Value.Free();
                this.MaterialInfoBuffer.Value = buffer.MaterialInfoBuffer.Value;
            }
            if (!object.ReferenceEquals(this.InstanceInfoBuffer.Value, buffer.InstanceInfoBuffer.Value))
            {
                this.InstanceInfoBuffer.Value.Free();
                this.InstanceInfoBuffer.Value = buffer.InstanceInfoBuffer.Value;
            }
            if (!object.ReferenceEquals(this.Instances.Value, buffer.Instances.Value))
            {
                this.Instances.Value = buffer.Instances.Value;
            }
            if (this.InstanceBoundingBox.Value != buffer.InstanceBoundingBox.Value)
            {
                this.InstanceBoundingBox.Value = buffer.InstanceBoundingBox.Value;
            }
            if (this.CullMode.Value != buffer.CullMode.Value)
            {
                this.CullMode.Value = buffer.CullMode.Value;
            }
            if (this.FillMode.Value != buffer.FillMode.Value)
            {
                this.FillMode.Value = buffer.FillMode.Value;
            }
        }

        public void Dispose()
        {
            VertexBuffer.Value.Free();
            IndexBuffer.Value.Free();
            MaterialInfoBuffer.Value.Free();
            InstanceInfoBuffer.Value.Free();
        }
    }
}

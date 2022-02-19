using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Buffers;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class MeshDeviceBuffer : IDisposable
    {
        public Mutable<PolygonFillMode> FillMode { get; }
        public Mutable<FaceCullMode> CullMode { get; }
        public Mutable<PooledDeviceBuffer?> MaterialInfoBuffer { get; }
        public Mutable<PooledDeviceBuffer> VertexBuffer { get; }
        public Mutable<PooledDeviceBuffer> IndexBuffer { get; }
        public Mutable<PooledDeviceBuffer?> BonesInfoBuffer { get; }
        public Mutable<PooledDeviceBuffer?> BoneTransformationBuffer { get; }
        public Mutable<uint> IndexLength { get; }
        public Mutable<BoundingBox> BoundingBox { get; }
        public Mutable<BoundingBox> InstanceBoundingBox { get; }
        public Mutable<VertexLayoutDescription> VertexLayout { get; }
        public Mutable<IndexFormat> IndexFormat { get; }
        public Mutable<PrimitiveTopology> PrimitiveTopology { get; }
        public Mutable<MaterialInfo?> Material { get; set; }
        public Mutable<TextureView?> TextureView { get; }
        public Mutable<TextureView?> AlphaMap { get; }
        public Mutable<IReadOnlyList<Matrix4x4>?> BoneTransforms { get; }
        public Mutable<IReadOnlyList<InstanceInfo>?> Instances { get; }
        public Mutable<PooledDeviceBuffer?> InstanceInfoBuffer { get; }
        public IBoneAnimationProvider[]? BoneAnimationProviders { get; set; } // Move?

        public MeshDeviceBuffer(
            PooledDeviceBuffer vertexBuffer, PooledDeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout, 
            IndexFormat indexFormat, PrimitiveTopology primitiveTopology, PooledDeviceBuffer? materialInfoBuffer = null, MaterialInfo? material = null, PooledDeviceBuffer? instanceInfoBuffer = null, InstanceInfo[]? instances = null, 
            TextureView? textureView = null, TextureView? alphaMap = null)
        {
            VertexBuffer = new Mutable<PooledDeviceBuffer>(vertexBuffer, this);
            IndexBuffer = new Mutable<PooledDeviceBuffer>(indexBuffer, this);
            IndexLength = new Mutable<uint>(indexLength, this);
            BoundingBox = new Mutable<BoundingBox>(boundingBox, this);
            VertexLayout = new Mutable<VertexLayoutDescription>(vertexLayout, this);
            IndexFormat = new Mutable<IndexFormat>(indexFormat, this);
            PrimitiveTopology = new Mutable<PrimitiveTopology>(primitiveTopology, this);
            Material = new Mutable<MaterialInfo?>(material, this);
            TextureView = new Mutable<TextureView?>(textureView, this);
            AlphaMap = new Mutable<TextureView?>(alphaMap, this);
            MaterialInfoBuffer = new Mutable<PooledDeviceBuffer?>(materialInfoBuffer, this);
            Instances = new Mutable<IReadOnlyList<InstanceInfo>?>(instances, this);
            InstanceInfoBuffer = new Mutable<PooledDeviceBuffer?>(instanceInfoBuffer, this);
            InstanceBoundingBox = new Mutable<BoundingBox>(CalculateInstanceBoundingBox(), this);
            FillMode = new Mutable<PolygonFillMode>(PolygonFillMode.Solid, this);
            CullMode = new Mutable<FaceCullMode>(FaceCullMode.None, this);
            BonesInfoBuffer = new Mutable<PooledDeviceBuffer?>(null, this);
            BoneTransformationBuffer = new Mutable<PooledDeviceBuffer?>(null, this);
            BoneTransforms = new Mutable<IReadOnlyList<Matrix4x4>?>(null, this);
        }

        public static MeshDeviceBuffer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, BaseMeshDataProvider mesh, TextureView? textureView = null, TextureView? alphaMap = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, BoundingBox? boundingBox = null)
        {
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory, deviceBufferPool, commandListPool);
            // TODO: fill bone info and bone transform
            //var boneInfoBuffer = 
            var realBoundingBox = boundingBox ?? mesh.GetBoundingBox();
            //TODO: make dynamic => reload bouding box when data changed, reload instance bounding box when instances changed etc

            var materialBuffer = mesh.Material == null ? null : resourceFactory.GetMaterialBuffer(graphicsDevice, mesh.Material.Value, deviceBufferPool);
            var instanceBuffer = resourceFactory.GetInstanceBuffer(graphicsDevice, mesh.Instances, deviceBufferPool);
            var bonesInfoBuffer = resourceFactory.GetBonesInfoBuffer(graphicsDevice, mesh.Bones, deviceBufferPool);
            var boneTransformBuffer = resourceFactory.GetBonesTransformBuffer(graphicsDevice, mesh.BoneTransforms, deviceBufferPool);

            var buffer = new MeshDeviceBuffer(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, realBoundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, materialBuffer, mesh.Material, instanceInfoBuffer: instanceBuffer, instances: mesh.Instances, textureView: textureView, alphaMap: alphaMap);
            buffer.BonesInfoBuffer.Value = bonesInfoBuffer;
            buffer.BoneTransformationBuffer.Value = boneTransformBuffer;
            buffer.BoneTransforms.Value = mesh.BoneTransforms;
            buffer.BoneAnimationProviders = mesh.BoneAnimationProviders;
            // TODO: update bone transforms?!

            buffer.Material.ValueChanged += (_, _) => UpdateMaterialBuffer(graphicsDevice, resourceFactory, buffer, deviceBufferPool);
            buffer.Instances.ValueChanged += (_, _) => UpdateInstances(graphicsDevice, resourceFactory, buffer, deviceBufferPool);
            buffer.BoneTransforms.ValueChanged += (_, _) => UpdateBoneTransformBuffer(graphicsDevice, resourceFactory, buffer, deviceBufferPool);
            buffer.BoundingBox.ValueChanged += (_, _) => buffer.InstanceBoundingBox.Value = buffer.CalculateInstanceBoundingBox();
            return buffer;
        }

        private static void UpdateBoneTransformBuffer(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDeviceBuffer buffer, DeviceBufferPool? deviceBufferPool = null)
        {
            if (buffer.BoneTransforms.Value == null)
                return;

            if (buffer.BoneTransformationBuffer.Value == null)
                buffer.BoneTransformationBuffer.Value = resourceFactory.GetBonesTransformBuffer(graphicsDevice, buffer.BoneTransforms.Value.ToArray(), deviceBufferPool);
            else
                graphicsDevice.UpdateBuffer(buffer.BoneTransformationBuffer.Value!.RealDeviceBuffer, 0, buffer.BoneTransforms.Value.ToArray());
        }

        private static void UpdateInstances(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDeviceBuffer buffer, DeviceBufferPool? deviceBufferPool = null)
        {
            if (buffer.Instances.Value == null)
                return;

            if (buffer.InstanceInfoBuffer.Value == null)
                buffer.InstanceInfoBuffer.Value = resourceFactory.GetInstanceBuffer(graphicsDevice, buffer.Instances.Value.ToArray(), deviceBufferPool);
            else
                graphicsDevice.UpdateBuffer(buffer.InstanceInfoBuffer.Value!.RealDeviceBuffer, 0, buffer.Instances.Value.ToArray());
        }

        private static void UpdateMaterialBuffer(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDeviceBuffer buffer, DeviceBufferPool? deviceBufferPool = null)
        {
            if (buffer.Material.Value == null)
                return;

            if (buffer.MaterialInfoBuffer.Value == null)
                buffer.MaterialInfoBuffer.Value = resourceFactory.GetMaterialBuffer(graphicsDevice, buffer.Material.Value.Value, deviceBufferPool);
            else 
                graphicsDevice.UpdateBuffer(buffer.MaterialInfoBuffer.Value!.RealDeviceBuffer, 0, buffer.Material.Value.Value);
        }

        private BoundingBox CalculateInstanceBoundingBox()
        {
            if (this.Instances.Value == null)
                return new BoundingBox();

            BoundingBox? current = null;
            foreach(var instance in this.Instances.Value)
            {
                var worldMatrix = new Transform(instance.Position, Matrix4x4.CreateRotationX(instance.Rotation.X) * Matrix4x4.CreateRotationY(instance.Rotation.Y) * Matrix4x4.CreateRotationZ(instance.Rotation.Z), instance.Scale).CreateWorldMatrix();
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
                this.MaterialInfoBuffer.Value?.Free();
                this.MaterialInfoBuffer.Value = buffer.MaterialInfoBuffer.Value;
            }
            if (!object.ReferenceEquals(this.InstanceInfoBuffer.Value, buffer.InstanceInfoBuffer.Value))
            {
                this.InstanceInfoBuffer.Value?.Free();
                this.InstanceInfoBuffer.Value = buffer.InstanceInfoBuffer.Value;
            }
            if (!object.ReferenceEquals(this.BonesInfoBuffer.Value, buffer.BonesInfoBuffer.Value))
            {
                this.BonesInfoBuffer.Value?.Free();
                this.BonesInfoBuffer.Value = buffer.BonesInfoBuffer.Value;
            }
            if (!object.ReferenceEquals(this.BoneTransformationBuffer.Value, buffer.BoneTransformationBuffer.Value))
            {
                this.BoneTransformationBuffer.Value?.Free();
                this.BoneTransformationBuffer.Value = buffer.BoneTransformationBuffer.Value;
            }
            if (!object.ReferenceEquals(this.Instances.Value, buffer.Instances.Value))
            {
                this.Instances.Value = buffer.Instances.Value;
            }
            if (!object.ReferenceEquals(this.BoneTransforms.Value, buffer.BoneTransforms.Value))
            {
                this.BoneTransforms.Value = buffer.BoneTransforms.Value;
            }
            if (!object.ReferenceEquals(this.BoneAnimationProviders, buffer.BoneAnimationProviders))
            {
                this.BoneAnimationProviders = buffer.BoneAnimationProviders;
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
            MaterialInfoBuffer.Value?.Free();
            InstanceInfoBuffer.Value?.Free();
            BonesInfoBuffer.Value?.Free();
            BoneTransformationBuffer.Value?.Free();
        }
    }
}

using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Buffers;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Mesh
{
    //public class AutoRefreshPhysicsMeshDeviceBuffer?<TShape> : PhysicsMeshDeviceBuffer<TShape>
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
                  buffer.Instances.Value?.ToArray(), buffer.TextureView.Value, buffer.AlphaMap.Value)
        {
            Shape = new Mutable<TShape>(shape, this);

            BoneAnimationProviders = buffer.BoneAnimationProviders;
            BonesInfoBuffer.Value = buffer.BonesInfoBuffer.Value;
            BoneTransforms.Value = buffer.BoneTransforms.Value;
            BoneTransformationBuffer.Value = buffer.BoneTransformationBuffer.Value;
        }

        public static PhysicsMeshDeviceBuffer<TShape> Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, BaseMeshDataProvider mesh, TShape shape, TextureView? textureView = null, TextureView? alphaMap = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, BoundingBox? boundingBox = null)
        {
            var baseBuffer = Create(graphicsDevice, resourceFactory, mesh, textureView, alphaMap, deviceBufferPool, commandListPool, boundingBox);
            return new PhysicsMeshDeviceBuffer<TShape>(baseBuffer, shape);
        }
    }
}

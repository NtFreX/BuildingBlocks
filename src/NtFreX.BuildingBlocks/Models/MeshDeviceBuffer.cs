using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public class PhysicsMeshDeviceBuffer<TShape> : MeshDeviceBuffer
        where TShape : unmanaged, IShape
    {
        public Func<Simulation, TShape> ShapeAllocator { get; set; }

        public PhysicsMeshDeviceBuffer(
            DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout,
            IndexFormat indexFormat, PrimitiveTopology primitiveTopology, Func<Simulation, TShape> shapeAllocator, MaterialInfo? material = null, TextureView? textureView = null)
            : base(vertexBuffer, indexBuffer, indexLength, boundingBox, vertexLayout, indexFormat, primitiveTopology, material, textureView)
        {
            ShapeAllocator = shapeAllocator;
        }

        public static PhysicsMeshDeviceBuffer<TShape> Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, Func<Simulation, TShape> shapeAllocator, TextureView? textureView = null)
        {
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory);
            var boundingBox = mesh.GetBoundingBox();
            return new PhysicsMeshDeviceBuffer<TShape>(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, boundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, shapeAllocator, mesh.Material, textureView: textureView);
        }
    }
    public class MeshDeviceBuffer
    {
        //TODO: make all updateable (model class needs to update)
        public DeviceBuffer VertexBuffer { get; }
        public DeviceBuffer IndexBuffer { get; }
        public uint IndexLength { get; }
        public BoundingBox BoundingBox { get; }
        public VertexLayoutDescription VertexLayout { get; }
        public IndexFormat IndexFormat { get; }
        public PrimitiveTopology PrimitiveTopology { get; }
        public Mutable<MaterialInfo> Material { get; set; }
        public Mutable<TextureView?> TextureView { get; }

        public MeshDeviceBuffer(
            DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout, 
            IndexFormat indexFormat, PrimitiveTopology primitiveTopology, MaterialInfo? material = null, TextureView? textureView = null)
        {
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            IndexLength = indexLength;
            BoundingBox = boundingBox;
            VertexLayout = vertexLayout;
            IndexFormat = indexFormat;
            PrimitiveTopology = primitiveTopology;
            Material = new Mutable<MaterialInfo>(material ?? new MaterialInfo());
            TextureView = new Mutable<TextureView?>(textureView);
        }

        public static MeshDeviceBuffer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, TextureView? textureView = null)
        {
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory);
            var boundingBox = mesh.GetBoundingBox();
            return new MeshDeviceBuffer(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, boundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, mesh.Material, textureView: textureView);
        }
    }
}

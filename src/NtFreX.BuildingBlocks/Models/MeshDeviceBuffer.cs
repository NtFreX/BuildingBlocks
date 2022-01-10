using BepuPhysics.Collidables;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
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
        public Triangle[]? Triangles { get; }
        public TextureView? TextureView { get; }

        public MaterialInfo Material { get; set; }

        public MeshDeviceBuffer(
            DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, uint indexLength, BoundingBox boundingBox, VertexLayoutDescription vertexLayout, 
            IndexFormat indexFormat, PrimitiveTopology primitiveTopology, MaterialInfo? material = null, Triangle[]? triangles = null, TextureView? textureView = null)
        {
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            IndexLength = indexLength;
            BoundingBox = boundingBox;
            VertexLayout = vertexLayout;
            IndexFormat = indexFormat;
            PrimitiveTopology = primitiveTopology;
            Material = material ?? new MaterialInfo();
            Triangles = triangles;
            TextureView = textureView;
        }

        public static MeshDeviceBuffer Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, MeshDataProvider mesh, Triangle[]? triangles = null, TextureView? textureView = null)
        {
            var buffers = mesh.BuildVertexAndIndexBuffer(graphicsDevice, resourceFactory);
            var boundingBox = mesh.GetBoundingBox();
            return new MeshDeviceBuffer(buffers.VertexBuffer, buffers.IndexBuffer, (uint)buffers.IndexCount, boundingBox, mesh.VertexLayout, mesh.IndexFormat, mesh.PrimitiveTopology, mesh.Material, triangles: triangles, textureView: textureView);
        }
    }
}

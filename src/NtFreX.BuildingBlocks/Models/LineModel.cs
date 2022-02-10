using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Models
{
    public static class LineModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, Index16> CreateMesh(Vector3 start, Vector3 end, float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, MaterialInfo? material = null)
        {
            var color = new RgbaFloat(red, green, blue, alpha);
            var vertices = new[] { new VertexPositionColorNormalTexture(start, color), new VertexPositionColorNormalTexture(end, color) };
            var indices = new Index16[] { 0, 1 };
            return new MeshDataProvider<VertexPositionColorNormalTexture, Index16>(vertices, indices, PrimitiveTopology.LineList, material: material);
        }

        public static Model Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders, Vector3 start, Vector3 end, ModelCreationInfo? creationInfo = null,
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, TextureView? texture = null, MaterialInfo? material = null, DeviceBufferPool? deviceBufferPool = null)
        {
            var mesh = CreateMesh(start, end, red, green, blue, alpha, material);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh, textureView: texture, creationInfo: creationInfo, deviceBufferPool: deviceBufferPool);
        }
    }
}

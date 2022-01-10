using BepuPhysics;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    static class LineModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, ushort> CreateMesh(Vector3 start, Vector3 end, float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, MaterialInfo? material = null)
        {
            var color = new RgbaFloat(red, green, blue, alpha);
            var vertices = new[] { new VertexPositionColorNormalTexture(start, color), new VertexPositionColorNormalTexture(end, color) };
            var indices = new ushort[] { 0, 1 };
            return new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(vertices, indices, IndexFormat.UInt16, PrimitiveTopology.LineList, VertexPositionColorNormalTexture.VertexLayout, material: material, bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition);
        }

        public static Model Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, ModelCreationInfo creationInfo, Shader[] shaders, Vector3 start, Vector3 end, 
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, TextureView? texture = null, MaterialInfo? material = null)
        {
            var mesh = CreateMesh(start, end, red, green, blue, alpha, material);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, simulation, creationInfo, shaders, mesh, texture);
        }
    }
}

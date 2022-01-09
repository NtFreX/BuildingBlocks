using BepuPhysics;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    static class LineModel
    {
        public static Model Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Simulation simulation, ModelCreationInfo creationInfo, Shader[] shaders, Vector3 start, Vector3 end, 
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, TextureView? texture = null, MaterialInfo? material = null)
        {
            var color = new RgbaFloat(red, green, blue, alpha);
            var vertices = new [] { new VertexPositionColorNormalTexture(start, color), new VertexPositionColorNormalTexture(end, color) };
            var indices = new ushort[] { 0, 1 };
            var mesh = new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(vertices, indices, vertex => vertex.Position, IndexFormat.UInt16);

            return new Model(graphicsDevice, resourceFactory, graphicsSystem, simulation, creationInfo, shaders, mesh, VertexPositionColorNormalTexture.VertexLayout, IndexFormat.UInt16, PrimitiveTopology.LineList, texture, material);
        }
    }
}

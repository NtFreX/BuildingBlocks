using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Models;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Sample.Models
{
    static class QubeModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, ushort> CreateMesh(float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float sideLength = 1f, MaterialInfo? material = null)
        {
            var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), sideLength / 2f);
            var indices = GetIndices();
            return new MeshDataProvider<VertexPositionColorNormalTexture, ushort>(
                vertices, indices, IndexFormat.UInt16, PrimitiveTopology.TriangleList,
                VertexPositionColorNormalTexture.VertexLayout, material: material,
                bytesBeforePosition: VertexPositionColorNormalTexture.BytesBeforePosition);
        }

        public static Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, ModelCreationInfo creationInfo, Shader[] shaders, 
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float sideLength = 1f, TextureView? texture = null, MaterialInfo? material = null,
            string? name = null)
        {
            var mesh = CreateMesh(red, green, blue, alpha, sideLength, material);
            var shapeAllocator = (Simulation simulation) => new Box(sideLength * creationInfo.Scale.X, sideLength * creationInfo.Scale.Y, sideLength * creationInfo.Scale.Z);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, creationInfo, shaders,  mesh, shapeAllocator, textureView: texture, name: name);
        }

        private static VertexPositionColorNormalTexture[] GetVertices(RgbaFloat color, float halfSideLength)
        {
            var vertexOne = new Vector3(-halfSideLength, +halfSideLength, -halfSideLength);
            var vertexTwo = new Vector3(+halfSideLength, +halfSideLength, -halfSideLength);
            var vertexThree = new Vector3(+halfSideLength, +halfSideLength, +halfSideLength);
            var vertexFour = new Vector3(-halfSideLength, +halfSideLength, +halfSideLength);
            var vertexFive = new Vector3(-halfSideLength, -halfSideLength, +halfSideLength);
            var vertexSix = new Vector3(-halfSideLength, -halfSideLength, -halfSideLength);
            var vertexSeven = new Vector3(+halfSideLength, -halfSideLength, -halfSideLength);
            var vertexEight = new Vector3(+halfSideLength, -halfSideLength, +halfSideLength);

            return new [] {
                // Top
                new VertexPositionColorNormalTexture(vertexOne, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexTwo, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexThree, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexFour, color, new Vector2(0, 1)),

                // Bottom 
                new VertexPositionColorNormalTexture(vertexFive, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexEight, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexSeven, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexSix, color, new Vector2(0, 1)),

                // Left  
                new VertexPositionColorNormalTexture(vertexOne, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexFour, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexFive, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexSix, color, new Vector2(0, 1)),

                // Right
                new VertexPositionColorNormalTexture(vertexThree, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexTwo, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexSeven, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexEight, color, new Vector2(0, 1)),

                // Back
                new VertexPositionColorNormalTexture(vertexTwo, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexOne, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexSix, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexSeven, color, new Vector2(0, 1)),

                // Front
                new VertexPositionColorNormalTexture(vertexFour, color, new Vector2(0, 0)),
                new VertexPositionColorNormalTexture(vertexThree, color, new Vector2(1, 0)),
                new VertexPositionColorNormalTexture(vertexEight, color, new Vector2(1, 1)),
                new VertexPositionColorNormalTexture(vertexFive, color, new Vector2(0, 1)),
            };
        }

        private static ushort[] GetIndices() => new ushort[] {                 
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23, 
        };
    }
}

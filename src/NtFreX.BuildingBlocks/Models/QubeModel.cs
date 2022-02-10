using BepuPhysics;
using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Models
{
    public static class QubeModel
    {
        public static MeshDataProvider<VertexPositionColorNormalTexture, Index16> CreateMesh(float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float sideLength = 1f, MaterialInfo? material = null)
        {
            var vertices = GetVertices(new RgbaFloat(red, green, blue, alpha), sideLength / 2f);
            var indices = GetIndices();
            return new MeshDataProvider<VertexPositionColorNormalTexture, Index16>(vertices, indices, PrimitiveTopology.TriangleList, material: material);
        }

        public static Model Create(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders, ModelCreationInfo? creationInfo = null,
            float red = 0f, float green = 0f, float blue = 0f, float alpha = 0f, float sideLength = 1f, TextureView? texture = null, MaterialInfo? material = null,
            string? name = null, DeviceBufferPool? deviceBufferPool = null)
        {
            var realCreationInfo = creationInfo ?? new ModelCreationInfo();
            var mesh = CreateMesh(red, green, blue, alpha, sideLength, material);
            var shape = new Box(sideLength * realCreationInfo.Scale.X, sideLength * realCreationInfo.Scale.Y, sideLength * realCreationInfo.Scale.Z);
            return Model.Create(graphicsDevice, resourceFactory, graphicsSystem, shaders, mesh, shape, creationInfo: realCreationInfo, textureView: texture, name: name, deviceBufferPool: deviceBufferPool);
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

        private static Index16[] GetIndices() => new Index16[] {                 
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23, 
        };
    }
}

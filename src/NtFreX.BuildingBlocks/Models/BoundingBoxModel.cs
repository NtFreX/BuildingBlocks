using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Models
{
    public static class BoundingBoxModel
    {
        public static Model CreateBoundingBoxModel(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Model model, Shader[] shaders, TextureView? texture, float opacity = .5f)
            => CreateBoundingBoxModel(graphicsDevice, resourceFactory, graphicsSystem, model.GetBoundingBox(), shaders, texture, opacity);

        public static Model CreateBoundingBoxModel(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, BoundingBox boundingBox, Shader[] shaders, TextureView? texture, float opacity = .5f)
        {
            var scaleX = boundingBox.Max.X - boundingBox.Min.X;
            var scaleY = boundingBox.Max.Y - boundingBox.Min.Y;
            var scaleZ = boundingBox.Max.Z - boundingBox.Min.Z;
            var posX = boundingBox.Min.X + scaleX / 2f;
            var posY = boundingBox.Min.Y + scaleY / 2f;
            var posZ = boundingBox.Min.Z + scaleZ / 2f;
            var bounds = QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, shaders, red: 1, creationInfo: new ModelCreationInfo { Position = new Vector3(posX, posY, posZ), Scale = new Vector3(scaleX, scaleY, scaleZ) }, texture: texture);
            bounds.MeshBuffer.Material.Value = bounds.MeshBuffer.Material.Value with { Opacity = opacity };
            return bounds;
        }
    }
}

using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model.Common;

// TODO: move to mesh namespace and rename to [xxx]mesh
public static class BoundingBoxModel
{
    public static MeshRenderer CreateBoundingBoxModel(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, CullRenderable model, TextureView? texture = null, float opacity = .5f)
        => CreateBoundingBoxModel(graphicsDevice, resourceFactory, graphicsSystem, model.GetBoundingBox(), texture, opacity);

    public static MeshRenderer CreateBoundingBoxModel(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, BoundingBox boundingBox, TextureView? texture = null, float opacity = .5f)
    {
        var scaleX = boundingBox.Max.X - boundingBox.Min.X;
        var scaleY = boundingBox.Max.Y - boundingBox.Min.Y;
        var scaleZ = boundingBox.Max.Z - boundingBox.Min.Z;
        var posX = boundingBox.Min.X + scaleX / 2f;
        var posY = boundingBox.Min.Y + scaleY / 2f;
        var posZ = boundingBox.Min.Z + scaleZ / 2f;
        var bounds = QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, red: 1, transform: new Transform { Position = new Vector3(posX, posY, posZ), Scale = new Vector3(scaleX, scaleY, scaleZ) }, texture: texture);
        bounds.MeshBuffer.Material.Value = bounds.MeshBuffer.Material.Value with { Opacity = opacity };
        return bounds;
    }
}


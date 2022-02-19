using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using NtFreX.BuildingBlocks.Texture.Text;
using SixLabors.Fonts;
using System.Buffers;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class TextModel
{
    //TODO: make collection model or something like that so this returns only one object instead of an array
    public static MeshRenderer[] Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Font font, string text, RgbaFloat color, Transform? transform = null, DeviceBufferPool ? deviceBufferPool = null)
        => Create(graphicsDevice, resourceFactory, graphicsSystem, new[] { (font, text, color) }, transform, deviceBufferPool);
    public static MeshRenderer[] Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, (Font Font, string Text, RgbaFloat Color)[] text, Transform? transform = null, DeviceBufferPool ? deviceBufferPool = null)
    {
        var buffer = new TextBuffer();
        foreach(var part in text)
        {
            buffer.Append(graphicsDevice, resourceFactory, part.Font, part.Text, part.Color);
        }

        var meshBuffer = buffer.Build(graphicsDevice, resourceFactory, out var meshBufferData, deviceBufferPool);
        var models = new MeshRenderer[meshBufferData.Length];
        for(var i = 0; i < meshBufferData.Length; i++)
        {
            models[i] = new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, meshBufferData[i], transform: transform);
        }
        ArrayPool<MeshDeviceBuffer>.Shared.Return(meshBuffer);

        return models;
    }
}

using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Texture;
using SixLabors.Fonts;
using System.Buffers;
using Veldrid;

namespace NtFreX.BuildingBlocks.Models
{
    public static class TextModel
    {
        //TODO: make collection model or something like that so this returns only one object instead of an array
        public static Model[] Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Font font, string text, RgbaFloat color, Shader[] shaders, ModelCreationInfo? creationInfo = null, DeviceBufferPool ? deviceBufferPool = null, IBehavior[]? behaviors = null)
            => Create(graphicsDevice, resourceFactory, graphicsSystem, new[] { (font, text, color) }, shaders, creationInfo, deviceBufferPool, behaviors);
        public static Model[] Create(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, (Font Font, string Text, RgbaFloat Color)[] text, Shader[] shaders, ModelCreationInfo? creationInfo = null, DeviceBufferPool ? deviceBufferPool = null, IBehavior[]? behaviors = null)
        {
            var buffer = new TextBuffer();
            foreach(var part in text)
            {
                buffer.Append(graphicsDevice, resourceFactory, part.Font, part.Text, part.Color);
            }

            var meshBuffer = buffer.Build(graphicsDevice, resourceFactory, out var meshBufferData, deviceBufferPool);
            var models = new Model[meshBufferData.Length];
            for(var i = 0; i < meshBufferData.Length; i++)
            {
                models[i] = new Model(graphicsDevice, resourceFactory, graphicsSystem, shaders, meshBufferData[i], creationInfo: creationInfo, behaviors: behaviors);
            }
            ArrayPool<MeshDeviceBuffer>.Shared.Return(meshBuffer);

            return models;
        }
    }
}

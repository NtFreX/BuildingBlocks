using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using SixLabors.Fonts;
using System.Buffers;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture
{

    //TODO: use everywhere?
    public struct Text : IEquatable<Text>
    {
        public Font Font { get; init; }
        public string Value { get; init; }
        public RgbaFloat Color { get; init; }

        public Text(Font font, string value, RgbaFloat color)
        {
            Font = font;
            Value = value;
            Color = color;
        }

        public override int GetHashCode() => (Font, Value, Color).GetHashCode();

        public override string ToString()
            => $"Font: {Font}, Color: {Color}, Value: {Value}";

        public bool Equals(Text other)
        {
            return other.Font == Font && other.Value == Value && other.Color == Color;
        }
    }
    public class TextModelBuffer : IDisposable
    {
        private readonly List<Model> textModels;
        private readonly GraphicsDevice graphicsDevice;
        private readonly ResourceFactory resourceFactory;
        private readonly GraphicsSystem graphicsSystem;
        private readonly Shader[] shaders;
        private readonly DeviceBufferPool? deviceBufferPool;
        private readonly CommandListPool? commandListPool;
        private readonly List<Text> currentText = new List<Text>();
        private readonly List<Func<Model, IBehavior>> behaviors = new List<Func<Model, IBehavior>>();

        private TextBuffer buffer = new TextBuffer();
        private Vector3 position = Vector3.Zero;
        private Vector3 scale = Vector3.One;
        private Quaternion rotation = Quaternion.Identity;

        public Text[] CurrentText => currentText.ToArray();

        public TextModelBuffer(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, Shader[] shaders, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.graphicsSystem = graphicsSystem;
            this.shaders = shaders;
            this.deviceBufferPool = deviceBufferPool;
            this.commandListPool = commandListPool;
            this.textModels = new List<Model>();
        }

        public void AddBehavoir(Func<Model, IBehavior> resolver)
        {
            behaviors.Add(resolver);
            foreach(var model in textModels)
            {
                model.AddBehavoirs(resolver);
            }
        }

        public void SetTransform(Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
        {
            if (position != null)
                this.position = position.Value;
            if (scale != null)
                this.scale = scale.Value;
            if (rotation != null)
                this.rotation = rotation.Value;

            for (var i = 0; i < textModels.Count; i++)
            {
                if (position != null)
                    textModels[i].Position.Value = position.Value;
                if (rotation != null)
                    textModels[i].Rotation.Value = rotation.Value;
                if (scale != null)
                    textModels[i].Scale.Value = scale.Value;
            }
        }

        public void Clear()
        {
            buffer = new TextBuffer();
            foreach (var model in textModels)
            {
                model.IsActive.Value = false;
            }
            currentText.Clear();
        }

        public void Write(Font font, string text, RgbaFloat color)
            => Write(new Text(font, text, color));

        public void Write(params Text[] data)
        {
            if (CurrentText.SequenceEqual(data))
                return;

            Clear();
            Append(data);
        }

        public void Append(Font font, string text, RgbaFloat color)
            => Append(new Text(font, text, color));

        public void Append(params Text[] data)
        {
            foreach (var record in data)
            {
                buffer.Append(graphicsDevice, resourceFactory, record.Font, record.Value, record.Color);
                currentText.Add(record);
            }
            var deviceBuffer = buffer.Build(graphicsDevice, resourceFactory, out var bufferData, deviceBufferPool, commandListPool);

            for (var i = 0; i < bufferData.Length; i++)
            {
                if (textModels.Count <= i)
                {
                    var newModel = new Model(graphicsDevice, resourceFactory, graphicsSystem, shaders, bufferData[i], creationInfo: new ModelCreationInfo { Position = position, Scale = scale, Rotation = rotation });
                    newModel.AddBehavoirs(behaviors.ToArray());
                    textModels.Add(newModel);
                    graphicsSystem.AddModels(newModel);
                }
                else
                {
                    textModels[i].IsActive.Value = true;
                    textModels[i].MeshBuffer.Set(bufferData[i]);
                }
            }

            ArrayPool<MeshDeviceBuffer>.Shared.Return(deviceBuffer);
        }

        public void Dispose()
        {
            foreach(var model in textModels)
            {
                model.Dispose();
            }
        }
    }
}

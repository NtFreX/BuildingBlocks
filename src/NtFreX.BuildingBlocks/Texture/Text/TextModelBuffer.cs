using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using SixLabors.Fonts;
using System.Buffers;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

public class TextModelBuffer : IDisposable
{
    private readonly List<MeshRenderer> textModels;
    private readonly Scene scene;
    private readonly GraphicsDevice graphicsDevice;
    private readonly ResourceFactory resourceFactory;
    private readonly GraphicsSystem graphicsSystem;
    private readonly DeviceBufferPool? deviceBufferPool;
    private readonly CommandListPool? commandListPool;
    private readonly Action<MeshRenderer> onMeshCreated;
    private readonly List<TextData> currentText = new List<TextData>();

    private TextBuffer buffer = new TextBuffer();
    private Vector3 position = Vector3.Zero;
    private Vector3 scale = Vector3.One;
    private Matrix4x4 rotation = Matrix4x4.Identity;

    public TextData[] CurrentText => currentText.ToArray();

    public TextModelBuffer(Scene scene, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, GraphicsSystem graphicsSystem, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, Action<MeshRenderer>? onMeshCreated = null)
    {
        this.scene = scene;
        this.graphicsDevice = graphicsDevice;
        this.resourceFactory = resourceFactory;
        this.graphicsSystem = graphicsSystem;
        this.deviceBufferPool = deviceBufferPool;
        this.commandListPool = commandListPool;
        this.onMeshCreated = onMeshCreated ?? new Action<MeshRenderer>(mesh => { });
        this.textModels = new List<MeshRenderer>();
    }

    public void SetTransform(Vector3? position = null, Vector3? scale = null, Matrix4x4? rotation = null)
    {
        if (position != null)
            this.position = position.Value;
        if (scale != null)
            this.scale = scale.Value;
        if (rotation != null)
            this.rotation = rotation.Value;

        for (var i = 0; i < textModels.Count; i++)
        {
            var oldTransform = textModels[i].Transform.Value;
            var newTransform = textModels[i].Transform.Value;
            if (position != null)
                newTransform = newTransform with { Position = position.Value };
            if (rotation != null)
                newTransform = newTransform with { Rotation = rotation.Value };
            if (scale != null)
                newTransform = newTransform with { Scale = scale.Value };

            if (!oldTransform.Equals(newTransform))
                textModels[i].Transform.Value = newTransform;
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
        => Write(new TextData(font, text, color));

    public void Write(params TextData[] data)
    {
        if (CurrentText.SequenceEqual(data))
            return;

        Clear();
        Append(data);
    }

    public void Append(Font font, string text, RgbaFloat color)
        => Append(new TextData(font, text, color));

    public void Append(params TextData[] data)
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
                var newModel = new MeshRenderer(graphicsDevice, resourceFactory, graphicsSystem, bufferData[i], transform: new Transform { Position = position, Scale = scale, Rotation = rotation });
                onMeshCreated(newModel);
                textModels.Add(newModel);
                scene.AddCullRenderables(newModel);
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

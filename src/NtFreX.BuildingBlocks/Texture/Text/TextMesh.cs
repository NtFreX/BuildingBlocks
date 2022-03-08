using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using SixLabors.Fonts;
using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

//TODO: make this a renderer (maybe generic composite renderer)
public class TextMesh
{
    private readonly List<MeshRenderer> textModels;
    private readonly Scene scene;
    private readonly FaceCullMode faceCullMode;
    private readonly DeviceBufferPool? deviceBufferPool;
    private readonly CommandListPool? commandListPool;

    private TextRendererBuilder builder;
    private Vector3 position = Vector3.Zero;
    private Vector3 scale = Vector3.One;
    private Matrix4x4 rotation = Matrix4x4.Identity;

    public TextData[] CurrentText => builder.CurrentText;

    public TextMesh(Scene scene, FaceCullMode faceCullMode = FaceCullMode.Front, Transform? transform = null, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null)
    {
        this.scene = scene;
        this.faceCullMode = faceCullMode;
        this.builder = new (faceCullMode);
        this.deviceBufferPool = deviceBufferPool;
        this.commandListPool = commandListPool;
        this.scale = transform?.Scale ?? Vector3.One;
        this.position = transform?.Position ?? Vector3.Zero;
        this.rotation = transform?.Rotation ?? Matrix4x4.Identity;
        this.textModels = new List<MeshRenderer>();
    }

    //TODO: fix this (it probably does not work because the vertices are not centered arround 0,0,0)
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
        builder = new TextRendererBuilder(faceCullMode);
    }

    public Task WriteAsync(Font font, string text, RgbaFloat color)
        => WriteAsync(new TextData(font, text, color));

    public async Task WriteAsync(params TextData[] data)
    {
        if (CurrentText.SequenceEqual(data))
            return;

        Clear();
        await AppendAsync(data);
    }

    public Task AppendAsync(Font font, string text, RgbaFloat color)
        => AppendAsync(new TextData(font, text, color));

    public async Task AppendAsync(params TextData[] data)
    {
        var textIndex = CurrentText.Length;

        foreach (var record in data)
        {
            builder.Append(record);
        }
        
        var providers = builder.Build(textIndex);
        var currentProviderIndex = 0;
        for (; currentProviderIndex < providers.Length && textIndex < textModels.Count; currentProviderIndex++, textIndex++)
        {
            //TODO get rid of cast
            var definedData = textModels[textIndex].MeshData as DefinedMeshData<VertexPositionNormalTextureColor, Index16>;
            var providerData = await providers[currentProviderIndex].GetAsync();
            var meshData = providerData.Item1 as DefinedMeshData<VertexPositionNormalTextureColor, Index16>;

            Debug.Assert(definedData != null);
            Debug.Assert(meshData != null);

            if (definedData.Vertices.Length >= meshData.Vertices.Length)
            {
                definedData.Vertices = meshData.Vertices;
                if (!textModels[textIndex].IsActive)
                    textModels[textIndex].IsActive.Value = true;
            }
            else
            {
                //TODO: cleanup for small meshrenderers from time to time
                textModels[textIndex].IsActive.Value = false;
                currentProviderIndex--;
            }
        }

        for(; textIndex < textModels.Count; textIndex++)
        {
            textModels[textIndex].IsActive.Value = false;
        }

        var transform = new Transform(position, rotation, scale);
        var newRenderers = new List<MeshRenderer>();
        for (; currentProviderIndex < providers.Length; currentProviderIndex++)
        {
            var bufferBuilder = new TextRendererBuilder.TextBufferBuilder();
            newRenderers.Add(await MeshRenderer.CreateAsync(providers[currentProviderIndex], transform: transform, deviceBufferPool: deviceBufferPool, commandListPool: commandListPool, meshBufferBuilder: bufferBuilder));
        }

        textModels.AddRange(newRenderers);
        await scene.AddCullRenderablesAsync(newRenderers.ToArray());
        scene.AddUpdateables(newRenderers.ToArray());
    }

    public void DestroyDeviceObjects()
    {
        scene.RemoveCullRenderables(textModels.ToArray());
        foreach (var model in textModels)
        {
            model.DestroyDeviceObjects();
        }
        textModels.Clear();
    }
}

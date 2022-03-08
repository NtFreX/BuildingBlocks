using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

// TODO: support custom samplers
public class SurfaceTextureMeshDataSpecialization : MeshDataSpecialization, IEquatable<SurfaceTextureMeshDataSpecialization>
{
    private GraphicsDevice? graphicsDevice;
    private ResourceFactory? resourceFactory;

    public Mutable<TextureProvider> TextureProvider { get; }

    public TextureView? TextureView { get; private set; }
    public ResourceSet? ResouceSet { get; private set; }

    public SurfaceTextureMeshDataSpecialization(TextureProvider textureProvider)
    {
        TextureProvider = new Mutable<TextureProvider>(textureProvider, this);
        TextureProvider.ValueChanged += (_, _) => UpdateTextureAsync().Wait(); // TODO: arg we need to await this
    }

    private async Task UpdateTextureAsync()
    {
        if (graphicsDevice == null || resourceFactory == null)
            return;

        TextureView = await TextureProvider.Value.GetAsync(graphicsDevice, resourceFactory);
    }

    public static bool operator !=(SurfaceTextureMeshDataSpecialization? one, SurfaceTextureMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(SurfaceTextureMeshDataSpecialization? one, SurfaceTextureMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => TextureProvider.Value?.GetHashCode() ?? 0;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(SurfaceTextureMeshDataSpecialization? other)
        => other?.TextureProvider.Value?.Equals(TextureProvider.Value) ?? false;

    public override async Task CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
    {
        if (this.graphicsDevice != null)
        {
            Debug.Assert(this.graphicsDevice == graphicsDevice);
            return;
        }

        this.graphicsDevice = graphicsDevice;
        this.resourceFactory = resourceFactory;

        Debug.Assert(TextureProvider.Value != null);

        await UpdateTextureAsync();
        Debug.Assert(TextureView != null);

        var layout = ResourceLayoutFactory.GetSurfaceTextureLayout(resourceFactory);
        ResouceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(layout, TextureView, graphicsDevice.Aniso4xSampler));
    }

    public override void DestroyDeviceObjects()
    {
        graphicsDevice = null;
        resourceFactory = null;

        ResouceSet?.Dispose();
        ResouceSet = null;

        TextureView?.Dispose();
        TextureView = null;
    }
}

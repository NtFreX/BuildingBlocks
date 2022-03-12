using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class SpecularMapMeshDataSpecialization : SurfaceTextureMeshDataSpecialization, IEquatable<SpecularMapMeshDataSpecialization>
{
    public SpecularMapMeshDataSpecialization(TextureProvider textureProvider)
        : base(textureProvider) { }

    public static bool operator !=(SpecularMapMeshDataSpecialization? one, SpecularMapMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(SpecularMapMeshDataSpecialization? one, SpecularMapMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => base.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(SpecularMapMeshDataSpecialization? other)
        => base.Equals(other);
}
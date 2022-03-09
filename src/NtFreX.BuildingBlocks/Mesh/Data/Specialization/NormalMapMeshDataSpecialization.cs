using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class NormalMapMeshDataSpecialization : SurfaceTextureMeshDataSpecialization, IEquatable<NormalMapMeshDataSpecialization>
{
    public NormalMapMeshDataSpecialization(TextureProvider textureProvider)
        : base(textureProvider) { }

    public static bool operator !=(NormalMapMeshDataSpecialization? one, NormalMapMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(NormalMapMeshDataSpecialization? one, NormalMapMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => base.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(NormalMapMeshDataSpecialization? other)
        => base.Equals(other);
}

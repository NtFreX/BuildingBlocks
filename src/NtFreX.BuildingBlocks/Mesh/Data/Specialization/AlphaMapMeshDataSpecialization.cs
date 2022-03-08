using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Texture;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

//TODO: solve specializations nicer so this ceremony class can be deleted
public class AlphaMapMeshDataSpecialization : SurfaceTextureMeshDataSpecialization, IEquatable<AlphaMapMeshDataSpecialization>
{
    public AlphaMapMeshDataSpecialization(TextureProvider textureProvider) 
        : base(textureProvider) { }

    public static bool operator !=(AlphaMapMeshDataSpecialization? one, AlphaMapMeshDataSpecialization? two)
        => !(one == two);

    public static bool operator ==(AlphaMapMeshDataSpecialization? one, AlphaMapMeshDataSpecialization? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => base.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(AlphaMapMeshDataSpecialization? other)
        => base.Equals(other);
}

using BepuPhysics.Collidables;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Physics;

//TODO: this probably should not be a MeshDataSpecialization (they are needed to create special graphics device resources) this is a sys mem resource (currently they should be all kept in memory) 
public class BepuPhysicsShapeMeshDataSpecialization<TShape> : MeshDataSpecialization, IEquatable<BepuPhysicsShapeMeshDataSpecialization<TShape>>
    where TShape : unmanaged, IShape
{
    public TShape Shape { get; set; }

    public BepuPhysicsShapeMeshDataSpecialization(TShape shape)
    {
        Shape = shape;
    }

    public static bool operator !=(BepuPhysicsShapeMeshDataSpecialization<TShape>? one, BepuPhysicsShapeMeshDataSpecialization<TShape>? two)
        => !(one == two);

    public static bool operator ==(BepuPhysicsShapeMeshDataSpecialization<TShape>? one, BepuPhysicsShapeMeshDataSpecialization<TShape>? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => Shape.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(BepuPhysicsShapeMeshDataSpecialization<TShape>? other)
    {   
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (!Shape.Equals(other.Shape))
            return false;

        return true;
    }
}

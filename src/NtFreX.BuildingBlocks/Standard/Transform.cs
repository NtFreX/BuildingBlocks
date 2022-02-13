using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard;

public struct Transform : IEquatable<Transform>
{
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Matrix4x4 Rotation { get; init; } = Matrix4x4.Identity;
    public Vector3 Scale { get; init; } = Vector3.One;

    public Transform() { }
    public Transform(Vector3 position, Matrix4x4 rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public Matrix4x4 CreateWorldMatrix()
    {
        return Matrix4x4.CreateScale(Scale) *
                Rotation *
                Matrix4x4.CreateTranslation(Position);
    }

    public override int GetHashCode()
        => (Position, Rotation, Scale).GetHashCode();

    public override string ToString()
        => $"Position: {Position}, Scale: {Scale}, Rotation: {Rotation}";

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(null, obj)) return false;
        if(obj.GetType() != typeof(Transform)) return false;
        return Equals((Transform)obj);
    }

    public bool Equals(Transform other)
        => Position == other.Position && Rotation == other.Rotation && Scale == other.Scale;
}

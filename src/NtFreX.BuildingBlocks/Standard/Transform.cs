using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard;

public struct Transform : IEquatable<Transform>
{
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Matrix4x4 Rotation { get; init; } = Matrix4x4.Identity; // TODO: use quaternion?
    public Vector3 Scale { get; init; } = Vector3.One;

    public static Transform operator *(Transform one, Transform two)
        => new Transform(one.CreateWorldMatrix() * two.CreateWorldMatrix());

    public Transform() { }
    public Transform(Matrix4x4 transform)
    {
        Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation);

        Position = translation;
        Scale = scale;
        Rotation = Matrix4x4.CreateFromQuaternion(rotation);
    }
    public Transform(Vector3? position = null, Matrix4x4? rotation = null, Vector3? scale = null)
    {
        Position = position ?? Vector3.Zero;
        Rotation = rotation ?? Matrix4x4.Identity;
        Scale = scale ?? Vector3.One;
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

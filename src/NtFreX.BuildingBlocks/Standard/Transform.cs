using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard;

public struct Transform : IEquatable<Transform>
{
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Matrix4x4 Rotation { get; init; } = Matrix4x4.Identity; // TODO: use quaternion or euler angles
    public Vector3 Scale { get; init; } = Vector3.One;

    //TODO make this correct!
    public static Transform operator *(Transform one, Transform two)
        => new (one.Position + two.Position, one.Rotation * two.Rotation, one.Scale * two.Scale); //new Transform(one.CreateWorldMatrix() * two.CreateWorldMatrix());

    public Transform() { }
    public Transform(Matrix4x4 transform)
    {
        //TODO: test this!
        Matrix4x4.Decompose(transform, out _, out var rotation, out _);

        Position = transform.GetPosition();
        Scale = transform.GetScale();
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

    public static bool operator !=(Transform? one, Transform? two)
        => !(one == two);

    public static bool operator ==(Transform? one, Transform? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode()
        => (Position, Rotation, Scale).GetHashCode();

    public override string ToString()
        => $"Position: {Position}, Scale: {Scale}, Rotation: {Rotation}";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(Transform other)
        => Position == other.Position && Rotation == other.Rotation && Scale == other.Scale;
}

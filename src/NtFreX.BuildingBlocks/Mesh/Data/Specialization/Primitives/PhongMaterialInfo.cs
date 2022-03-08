using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;

public struct PhongMaterialInfo : IEquatable<PhongMaterialInfo>
{
    public float Opacity { get; init; }
    public float Shininess { get; init; }
    public float ShininessStrength { get; init; }    
    public float Reflectivity { get; init; }
    public Vector4 AmbientColor { get; init; }
    public Vector4 DiffuseColor { get; init; }
    public Vector4 EmissiveColor { get; init; }
    public Vector4 ReflectiveColor { get; init; }
    public Vector4 SpecularColor { get; init; }
    public Vector4 TransparentColor { get; init; }

    public PhongMaterialInfo()
    {
        this.Opacity = 1f;
        this.Shininess = 0f;
        this.ShininessStrength = 0.2f;
        this.Reflectivity = 0f;
        this.AmbientColor = new Vector4(0, 0, 0, 1);
        this.DiffuseColor = new Vector4(0, 0, 0, 1);
        this.EmissiveColor = new Vector4(0, 0, 0, 1);
        this.ReflectiveColor = new Vector4(0, 0, 0, 1);
        this.SpecularColor = new Vector4(0, 0, 0, 1);
        this.TransparentColor = Vector4.Zero;
    }

    public PhongMaterialInfo(float opacity = 1f, float shininess = 0f, float shininessStrength = 0.2f, float reflectivity = 0f, 
        Vector4? ambientColor = null, Vector4? diffuseColor = null, Vector4? emissiveColor = null, Vector4? reflectiveColor = null,
        Vector4? specularColor = null, Vector4? transparentColor = null)
    {
        this.Opacity = opacity;
        this.Shininess = shininess;
        this.ShininessStrength = shininessStrength;
        this.Reflectivity = reflectivity;
        this.AmbientColor = ambientColor ?? new Vector4(0, 0, 0, 1);
        this.DiffuseColor = diffuseColor ?? new Vector4(0, 0, 0, 1);
        this.EmissiveColor = emissiveColor ?? new Vector4(0, 0, 0, 1);
        this.ReflectiveColor = reflectiveColor ?? new Vector4(0, 0, 0, 1);
        this.SpecularColor = specularColor ?? new Vector4(0, 0, 0, 1);
        this.TransparentColor = transparentColor ?? Vector4.Zero;
    }

    public static bool operator !=(PhongMaterialInfo? one, PhongMaterialInfo? two)
        => !(one == two);

    public static bool operator ==(PhongMaterialInfo? one, PhongMaterialInfo? two)
        => EqualsExtensions.EqualsValueType(one, two);

    public override int GetHashCode() 
        => (Opacity, Shininess, ShininessStrength, Reflectivity, AmbientColor, DiffuseColor, EmissiveColor, ReflectiveColor, SpecularColor, TransparentColor).GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(PhongMaterialInfo other)
    {
        return
            this.Opacity == other.Opacity &&
            this.Shininess == other.Shininess &&
            this.ShininessStrength == other.ShininessStrength &&
            this.Reflectivity == other.Reflectivity &&
            this.AmbientColor == other.AmbientColor &&
            this.DiffuseColor == other.DiffuseColor &&
            this.EmissiveColor == other.EmissiveColor &&
            this.ReflectiveColor == other.ReflectiveColor &&
            this.SpecularColor == other.SpecularColor &&
            this.TransparentColor == other.TransparentColor;
    }

    public override string ToString()
        => $"Opacity: {Opacity}, Shininess: {Shininess}, ShininessStrength: {ShininessStrength}, Reflectivity: {Reflectivity}, AmbientColor: {AmbientColor}, DiffuseColor: {DiffuseColor}, " + 
           $"EmissiveColor: {EmissiveColor}, ReflectiveColor: {ReflectiveColor}, SpecularColor: {SpecularColor}, TransparentColor: {TransparentColor}";
}
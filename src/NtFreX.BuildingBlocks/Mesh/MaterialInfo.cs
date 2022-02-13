using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Mesh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo : IEquatable<MaterialInfo>
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

        public MaterialInfo()
        {
            this.Opacity = 1f;
            this.Shininess = 0f;
            this.ShininessStrength = 0.2f;
            this.Reflectivity = 0f;
            this.AmbientColor = Vector4.Zero;
            this.DiffuseColor = Vector4.Zero;
            this.EmissiveColor = Vector4.Zero;
            this.ReflectiveColor = Vector4.Zero;
            this.SpecularColor = Vector4.Zero;
            this.TransparentColor = Vector4.Zero;
        }

        public MaterialInfo(float opacity = 1f, float shininess = 0f, float shininessStrength = 0.2f, float reflectivity = 0f, 
            Vector4? ambientColor = null, Vector4? diffuseColor = null, Vector4? emissiveColor = null, Vector4? reflectiveColor = null,
            Vector4? specularColor = null, Vector4? transparentColor = null)
        {
            this.Opacity = opacity;
            this.Shininess = shininess;
            this.ShininessStrength = shininessStrength;
            this.Reflectivity = reflectivity;
            this.AmbientColor = ambientColor ?? Vector4.Zero;
            this.DiffuseColor = diffuseColor ?? Vector4.Zero;
            this.EmissiveColor = emissiveColor ?? Vector4.Zero;
            this.ReflectiveColor = reflectiveColor ?? Vector4.Zero;
            this.SpecularColor = specularColor ?? Vector4.Zero;
            this.TransparentColor = transparentColor ?? Vector4.Zero;
        }

        public override int GetHashCode() => (Opacity, Shininess, ShininessStrength, Reflectivity, AmbientColor, DiffuseColor, EmissiveColor, ReflectiveColor, SpecularColor, TransparentColor).GetHashCode();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var objType = obj.GetType();
            if (objType != typeof(MaterialInfo)) return false;
            return Equals((MaterialInfo)obj);
        }

        public bool Equals(MaterialInfo other)
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
        {
            return $"Opacity: {Opacity}, Shininess: {Shininess}, ShininessStrength: {ShininessStrength}, Reflectivity: {Reflectivity}, AmbientColor: {AmbientColor}, DiffuseColor: {DiffuseColor}, EmissiveColor: {EmissiveColor}, ReflectiveColor: {ReflectiveColor}, SpecularColor: {SpecularColor}, TransparentColor: {TransparentColor}";
        }
        // ShadingMode
        // Texture...
    }
}

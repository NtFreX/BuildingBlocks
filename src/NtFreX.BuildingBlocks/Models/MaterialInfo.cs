using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public float Opacity = 1f;
        public float Shininess = 0f;
        public float ShininessStrength = .2f;
        public float Reflectivity = 0f;
        public Vector4 AmbientColor = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 DiffuseColor = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 EmissiveColor = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 ReflectiveColor = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 SpecularColor = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 TransparentColor = new Vector4(0f, 0f, 0f, 0f);

        // ShadingMode
        // Texture...
    }
}

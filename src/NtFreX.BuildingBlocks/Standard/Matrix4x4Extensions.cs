using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard
{
    public static class Matrix4x4Extensions
    {
        public static bool ContainsNaN(this Matrix4x4 matrix)
        {
            return matrix.M11 == float.NaN || matrix.M12 == float.NaN || matrix.M13 == float.NaN || matrix.M14 == float.NaN ||
                   matrix.M21 == float.NaN || matrix.M22 == float.NaN || matrix.M23 == float.NaN || matrix.M24 == float.NaN ||
                   matrix.M31 == float.NaN || matrix.M32 == float.NaN || matrix.M33 == float.NaN || matrix.M34 == float.NaN ||
                   matrix.M41 == float.NaN || matrix.M42 == float.NaN || matrix.M43 == float.NaN || matrix.M44 == float.NaN;
        }
    }
}

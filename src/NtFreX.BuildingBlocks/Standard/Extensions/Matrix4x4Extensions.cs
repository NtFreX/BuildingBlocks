using System.Numerics;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class Matrix4x4Extensions
{
    public static unsafe Assimp.Matrix4x4 ToAssimpMatrix(this Matrix4x4 mat)
    {
        return Unsafe.Read<Assimp.Matrix4x4>(&mat);
    }

    public static bool ContainsNaN(this Matrix4x4 matrix)
    {
        return matrix.M11 == float.NaN || matrix.M12 == float.NaN || matrix.M13 == float.NaN || matrix.M14 == float.NaN ||
                matrix.M21 == float.NaN || matrix.M22 == float.NaN || matrix.M23 == float.NaN || matrix.M24 == float.NaN ||
                matrix.M31 == float.NaN || matrix.M32 == float.NaN || matrix.M33 == float.NaN || matrix.M34 == float.NaN ||
                matrix.M41 == float.NaN || matrix.M42 == float.NaN || matrix.M43 == float.NaN || matrix.M44 == float.NaN;
    }

    public static Matrix4x4 CreatePerspective(bool isClipSpaceYInverted, bool useReverseDepth, float fov, float aspectRatio, float near, float far)
    {
        Matrix4x4 persp;
        if (useReverseDepth)
        {
            persp = CreatePerspective(fov, aspectRatio, far, near);
        }
        else
        {
            persp = CreatePerspective(fov, aspectRatio, near, far);
        }
        if (isClipSpaceYInverted)
        {
            persp *= new Matrix4x4(
                1, 0, 0, 0,
                0, -1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

        return persp;
    }

    public static Matrix4x4 CreatePerspective(float fov, float aspectRatio, float near, float far)
    {
        if (fov <= 0.0f || fov >= MathF.PI)
            throw new ArgumentOutOfRangeException(nameof(fov));

        if (near <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(near));

        if (far <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(far));

        float yScale = 1.0f / MathF.Tan(fov * 0.5f);
        float xScale = yScale / aspectRatio;

        Matrix4x4 result;

        result.M11 = xScale;
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = yScale;
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M31 = result.M32 = 0.0f;
        var negFarRange = float.IsPositiveInfinity(far) ? -1.0f : far / (near - far);
        result.M33 = negFarRange;
        result.M34 = -1.0f;

        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = near * negFarRange;

        return result;
    }
}

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
        return float.IsNaN(matrix.M11) || float.IsNaN(matrix.M12) || float.IsNaN(matrix.M13) || float.IsNaN(matrix.M14) ||
               float.IsNaN(matrix.M21) || float.IsNaN(matrix.M22) || float.IsNaN(matrix.M23) || float.IsNaN(matrix.M24) ||
               float.IsNaN(matrix.M31) || float.IsNaN(matrix.M32) || float.IsNaN(matrix.M33) || float.IsNaN(matrix.M34) ||
               float.IsNaN(matrix.M41) || float.IsNaN(matrix.M42) || float.IsNaN(matrix.M43) || float.IsNaN(matrix.M44);
    }

    public static Vector3 GetPosition(this Matrix4x4 matrix4X4)
        => new(matrix4X4.M14, matrix4X4.M24, matrix4X4.M34);
    public static Vector3 GetScale(this Matrix4x4 matrix4X4)
        => new(matrix4X4.M11, matrix4X4.M22, matrix4X4.M33);

    public static Matrix4x4 CreateOrthographic(bool isClipSpaceYInverted, bool useReverseDepth, float left, float right, float bottom, float top, float near, float far)
    {
        Matrix4x4 ortho;
        if (useReverseDepth)
        {
            ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, far, near);
        }
        else
        {
            ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, near, far);
        }
        if (isClipSpaceYInverted)
        {
            ortho *= new Matrix4x4(
                1, 0, 0, 0,
                0, -1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

        return ortho;
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

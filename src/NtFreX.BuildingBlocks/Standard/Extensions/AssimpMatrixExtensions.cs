using SystemMatrix4x4 = System.Numerics.Matrix4x4;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class AssimpMatrixExtensions
{
    public static unsafe SystemMatrix4x4 ToSystemMatrix(this AssimpMatrix4x4 matrix)
    {
        return Unsafe.Read<SystemMatrix4x4>(&matrix);
    }
}


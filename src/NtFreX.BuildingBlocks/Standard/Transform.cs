using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard
{
    public static class Transform
    {
        public static Matrix4x4 CreateWorldMatrix(Vector3 position, Matrix4x4 rotation, Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale) *
                    rotation *
                    Matrix4x4.CreateTranslation(position);
        }
    }
}

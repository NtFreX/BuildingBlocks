namespace NtFreX.BuildingBlocks.Standard
{
    public static class AssimpMatrixExtensions
    {
        public static System.Numerics.Matrix4x4 ToNumericsMatrix(this Assimp.Matrix4x4 matrix)
        {
            return new System.Numerics.Matrix4x4(
                    matrix.A1, matrix.A2, matrix.A3, matrix.A4,
                    matrix.B1, matrix.B2, matrix.B3, matrix.B4,
                    matrix.C1, matrix.C2, matrix.C3, matrix.C4,
                    matrix.D1, matrix.D2, matrix.D3, matrix.D4);
        }
    }
}

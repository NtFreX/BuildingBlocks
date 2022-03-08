namespace NtFreX.BuildingBlocks.Mesh.Common;

public static class Quad
{
    public static float[] GetFullScreenQuadVerts(bool isClipSpaceYInverted)
    {
        if (isClipSpaceYInverted)
        {
            return new float[]
            {
                        -1, -1, 0, 0,
                        1, -1, 1, 0,
                        1, 1, 1, 1,
                        -1, 1, 0, 1
            };
        }
        else
        {
            return new float[]
            {
                        -1, 1, 0, 0,
                        1, 1, 1, 0,
                        1, -1, 1, 1,
                        -1, -1, 0, 1
            };
        }
    }
}

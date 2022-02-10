using Assimp;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard
{
    public static class AssimpVector4DExtensions
    {
        public static Vector4 ToSystemVector(this Color4D vector)
            => new Vector4(vector.R, vector.G, vector.B, vector.A);
    }
}

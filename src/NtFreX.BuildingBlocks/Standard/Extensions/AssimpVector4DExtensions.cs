using Assimp;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class AssimpVector4DExtensions
{
    public static Vector4 ToSystemVector(this Color4D vector)
        => new (vector.R, vector.G, vector.B, vector.A);
}

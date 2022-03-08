using NtFreX.BuildingBlocks.Mesh.Primitives;
using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh
{
    public struct BoneInfoVertex : IEquatable<BoneInfoVertex>
    {
        public Vector4 BoneWeights;
        public UInt4 BoneIndices;

        public void AddBone(uint id, float weight)
        {
            if (BoneWeights.X == 0)
            {
                BoneWeights.X = weight;
                BoneIndices.X = id;
            }
            else if (BoneWeights.Y == 0)
            {
                BoneWeights.Y = weight;
                BoneIndices.Y = id;
            }
            else if (BoneWeights.Z == 0)
            {
                BoneWeights.Z = weight;
                BoneIndices.Z = id;
            }
            else if (BoneWeights.W == 0)
            {
                BoneWeights.W = weight;
                BoneIndices.W = id;
            }
        }

        public static bool operator !=(BoneInfoVertex? one, BoneInfoVertex? two)
            => !(one == two);

        public static bool operator ==(BoneInfoVertex? one, BoneInfoVertex? two)
            => EqualsExtensions.EqualsValueType(one, two);

        public override int GetHashCode() 
            => (BoneWeights, BoneIndices).GetHashCode();

        public override bool Equals([NotNullWhen(true)] object? obj)
            => EqualsExtensions.EqualsObject(this, obj);

        public bool Equals(BoneInfoVertex other)
            => BoneWeights == other.BoneWeights && BoneIndices == other.BoneIndices;

        public override string ToString()
            => $"BoneWeights: {BoneWeights}, BoneIndices: {BoneIndices}";
    }
}

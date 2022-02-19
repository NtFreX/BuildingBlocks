using NtFreX.BuildingBlocks.Mesh.Primitives;
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
        {
            if (!one.HasValue && !two.HasValue)
                return true;
            if (!one.HasValue)
                return false;
            if (!two.HasValue)
                return false;
            return one.Equals(two);
        }

        public override int GetHashCode() => (BoneWeights, BoneIndices).GetHashCode();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var objType = obj.GetType();
            if (objType != typeof(BoneInfoVertex)) return false;
            return Equals((BoneInfoVertex)obj);
        }

        public bool Equals(BoneInfoVertex other)
        {
            return
                BoneWeights == other.BoneWeights &&
                BoneIndices == other.BoneIndices;
        }

        public override string ToString()
        {
            return $"BoneWeights: {BoneWeights}, BoneIndices: {BoneIndices}";
        }
    }
}

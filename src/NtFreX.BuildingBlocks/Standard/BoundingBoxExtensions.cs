using System.Numerics;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Standard
{
    public static class BoundingBoxExtensions
    {
        public static bool ContainsNotOrIsNotSame(this in BoundingBox bigger, in BoundingBox smaller)
        {
            return bigger.Min.X > smaller.Min.X || bigger.Max.X < smaller.Max.X
                || bigger.Min.Y > smaller.Min.Y || bigger.Max.Y < smaller.Max.Y
                || bigger.Min.Z > smaller.Min.Z || bigger.Max.Z < smaller.Max.Z;
        }

        public static unsafe BoundingBox TransformBoundingBox(this in BoundingBox box, in Quaternion rotation, in Vector3 scale, in Vector3 position)
        {
            AlignedBoxCorners corners = box.GetCorners();
            Vector3* cornersPtr = (Vector3*)&corners;

            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            for (int i = 1; i < 8; i++)
            {
                Vector3 pos = Vector3.Transform(cornersPtr[i], rotation);

                if (min.X > pos.X) min.X = pos.X;
                if (max.X < pos.X) max.X = pos.X;

                if (min.Y > pos.Y) min.Y = pos.Y;
                if (max.Y < pos.Y) max.Y = pos.Y;

                if (min.Z > pos.Z) min.Z = pos.Z;
                if (max.Z < pos.Z) max.Z = pos.Z;
            }

            return new BoundingBox((min * scale) + position, (max * scale) + position);
        }
    }
}

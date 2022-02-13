using System.Numerics;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model;

public abstract class CullRenderable : Renderable
{
    public event EventHandler? NewBoundingBoxAvailable;

    // TODO: ability to hook up bounding box prediction of moving objects from bepu?

    /// <summary>
    /// If the new calculated bounding box is still within the spacer of the last published bounding box
    /// no NewBoundingBoxAvailable event will be published and the new calculated boundingbox will be discarded
    /// this is to safe performance updating the octree containing all frustum renderables
    /// </summary>
    public Vector3 BoundingBoxSpacer { get; set; } = Vector3.One * 50;

    public abstract BoundingBox GetBoundingBox();
    public abstract Vector3 GetCenter();

    public void PublishNewBoundingBoxAvailable()
        => NewBoundingBoxAvailable?.Invoke(this, EventArgs.Empty);
}

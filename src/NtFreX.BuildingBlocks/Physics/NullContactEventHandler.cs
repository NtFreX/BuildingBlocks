using BepuPhysics.CollisionDetection;

namespace NtFreX.BuildingBlocks.Physics
{
    public class NullContactEventHandler : IContactEventHandler
    {
        public void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        { }
    }
}

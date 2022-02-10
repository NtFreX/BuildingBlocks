using BepuPhysics.CollisionDetection;

namespace NtFreX.BuildingBlocks.Physics
{
    public interface IContactEventHandler
    {
        void HandleContact<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : struct, IContactManifold<TManifold>;
    }
}

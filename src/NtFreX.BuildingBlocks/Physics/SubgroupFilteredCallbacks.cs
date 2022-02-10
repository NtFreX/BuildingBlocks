using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Physics
{
    public struct SubgroupFilteredCallbacks : INarrowPhaseCallbacks
    {
        private readonly IContactEventHandler contactEventHandler;
        private readonly CollidableProperty<SubgroupCollisionFilter> collisionFilters;

        public SubgroupFilteredCallbacks(IContactEventHandler contactEventHandler, CollidableProperty<SubgroupCollisionFilter> collisionFilters)
        {
            this.contactEventHandler = contactEventHandler;
            this.collisionFilters = collisionFilters;
        }

        public void Initialize(Simulation simulation)
        {
            collisionFilters.Initialize(simulation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            //It's impossible for two statics to collide, and pairs are sorted such that bodies always come before statics.
            if (b.Mobility != CollidableMobility.Static)
            {
                return SubgroupCollisionFilter.AllowCollision(collisionFilters[a.BodyHandle], collisionFilters[b.BodyHandle]);
            }
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            //TODO: rad from physics material
            pairMaterial.FrictionCoefficient = 1.5f;
            pairMaterial.MaximumRecoveryVelocity = 1f;
            pairMaterial.SpringSettings = new SpringSettings(30, 0.5f);
            contactEventHandler.HandleContact(pair, manifold);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
            collisionFilters.Dispose();
        }
    }
}

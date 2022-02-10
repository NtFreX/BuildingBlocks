using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using System.Runtime.CompilerServices;

namespace NtFreX.BuildingBlocks.Physics
{
    public struct NullNarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        private readonly IContactEventHandler contactEventHandler;

        public NullNarrowPhaseCallbacks(IContactEventHandler contactEventHandler)
        {
            this.contactEventHandler = contactEventHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            => pair.A.Mobility == CollidableMobility.Dynamic || pair.B.Mobility == CollidableMobility.Dynamic;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = 0.1f;
            pairMaterial.MaximumRecoveryVelocity = 1f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1f);
            contactEventHandler.HandleContact(pair, manifold);
            return true;
        }

        public void Dispose() { }

        public void Initialize(Simulation simulation) { }
    }
}

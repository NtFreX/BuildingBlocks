using BepuPhysics;
using BepuUtilities;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Physics
{
    public struct NullPoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        //Vector3 gravityWideDt;
        //float linearDampingDt;
        //float angularDampingDt;
        Vector3Wide gravityWideDt;
        Vector<float> linearDampingDt;
        Vector<float> angularDampingDt;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public bool AllowSubstepsForUnconstrainedBodies => true;

        public bool IntegrateVelocityForKinematics => true;

        public void Initialize(Simulation simulation) { }

        //public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
        //{
        //    velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
        //    velocity.Angular = velocity.Angular * angularDampingDt;
        //}

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
            velocity.Angular = velocity.Angular * angularDampingDt; // gravityWideDt * new Vector<float>(100);
        }

        public void PrepareForIntegration(float dt)
        {
            const float linearDamping = 0.01f;
            const float angularDamping = 0.01f;
            Vector3 gravity = new Vector3(0, -9f, 0);

            //linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - linearDamping, 0, 1), dt));
            //angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - angularDamping, 0, 1), dt));
            //gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
            linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - linearDamping, 0, 1), dt));
            angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - angularDamping, 0, 1), dt));
            gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
        }
    }
}

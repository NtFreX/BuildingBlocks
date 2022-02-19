using Assimp;

using Matrix4x4 = System.Numerics.Matrix4x4;
using aiMatrix4x4 = Assimp.Matrix4x4;
using aiQuaternion = Assimp.Quaternion;
using NtFreX.BuildingBlocks.Standard.Extensions;

namespace NtFreX.BuildingBlocks.Mesh;

public class AssimpBoneAnimationProvider : IBoneAnimationProvider
{
    public bool IsRunning { get; set; }

    public Matrix4x4[] Transforms { get; }

    private double previousAnimSeconds = 0;
    private float animationTimeScale = 1f;

    private readonly Animation owningAnimation;
    private readonly List<NodeAnimationChannel> channels;
    private readonly Dictionary<string, uint> boneNames;
    private readonly aiMatrix4x4[] offsets;
    private readonly Node rootNode;
    private readonly aiMatrix4x4 rootInverse;
    private readonly aiMatrix4x4[] currentPassTransforms;

    public AssimpBoneAnimationProvider(Matrix4x4[] transforms, Animation owningAnimation, List<NodeAnimationChannel> channels, Dictionary<string, uint> boneNames, aiMatrix4x4[] offsets, Node rootNode, aiMatrix4x4 rootInverse)
    {
        Transforms = transforms;

        this.currentPassTransforms = new aiMatrix4x4[boneNames.Count];

        this.rootNode = rootNode;
        this.rootInverse = rootInverse;
        this.owningAnimation = owningAnimation;
        this.channels = channels;
        this.boneNames = boneNames;
        this.offsets = offsets;
    }

    // TODO: performance
    // TODO: make this work
    public void UpdateAnimation(float deltaSeconds)
    {
        double totalSeconds = owningAnimation.DurationInTicks * owningAnimation.TicksPerSecond;
        double newSeconds = previousAnimSeconds + (deltaSeconds * animationTimeScale);
        newSeconds = newSeconds % totalSeconds;
        previousAnimSeconds = newSeconds;

        double ticks = newSeconds * owningAnimation.TicksPerSecond;

        UpdateChannel(ticks, rootNode, aiMatrix4x4.Identity);

        for(var i = 0; i < currentPassTransforms.Length; i++)
        {
            Transforms[i] = Matrix4x4.Transpose(currentPassTransforms[i].ToSystemMatrix());
        }
    }

    private void UpdateChannel(double time, Node node, aiMatrix4x4 parentTransform)
    {
        var nodeTransformation = node.Transform;

        if (GetChannel(node, out NodeAnimationChannel? channel) && channel != null)
        {
            var scale = InterpolateScale(time, channel);
            var rotation = InterpolateRotation(time, channel);
            var translation = InterpolateTranslation(time, channel);

            nodeTransformation = scale * rotation * translation;
        }

        var inverseBase = node.Transform;
        inverseBase.Inverse();

        if (boneNames.TryGetValue(node.Name, out uint boneID))
        {
            currentPassTransforms[boneID] = offsets[(int)boneID] *
                nodeTransformation *
                rootInverse *
                inverseBase;
        }

        foreach (Node childNode in node.Children)
        {
            UpdateChannel(time, childNode, nodeTransformation * parentTransform);
        }
    }

    private bool GetChannel(Node node, out NodeAnimationChannel? channel)
    {
        foreach (NodeAnimationChannel c in channels)
        {
            if (c.NodeName == node.Name)
            {
                channel = c;
                return true;
            }
        }

        channel = null;
        return false;
    }

    private aiMatrix4x4 InterpolateTranslation(double time, NodeAnimationChannel channel)
    {
        Vector3D position;

        if (channel.PositionKeyCount == 1)
        {
            position = channel.PositionKeys[0].Value;
        }
        else
        {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.PositionKeyCount - 1; i++)
            {
                if (time < (float)channel.PositionKeys[(int)(i + 1)].Time)
                {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.PositionKeys[(int)frameIndex];
            VectorKey nextFrame = channel.PositionKeys[(int)((frameIndex + 1) % channel.PositionKeyCount)];

            double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;
            position = (start + (float)delta * (end - start));
        }

        return aiMatrix4x4.FromTranslation(position);
    }

    private aiMatrix4x4 InterpolateRotation(double time, NodeAnimationChannel channel)
    {
        aiQuaternion rotation;

        if (channel.RotationKeyCount == 1)
        {
            rotation = channel.RotationKeys[0].Value;
        }
        else
        {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.RotationKeyCount - 1; i++)
            {
                if (time < (float)channel.RotationKeys[(int)(i + 1)].Time)
                {
                    frameIndex = i;
                    break;
                }
            }

            QuaternionKey currentFrame = channel.RotationKeys[(int)frameIndex];
            QuaternionKey nextFrame = channel.RotationKeys[(int)((frameIndex + 1) % channel.RotationKeyCount)];

            double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            aiQuaternion start = currentFrame.Value;
            aiQuaternion end = nextFrame.Value;
            rotation = aiQuaternion.Slerp(start, end, (float)delta);
            rotation.Normalize();
        }

        return rotation.GetMatrix();
    }

    private aiMatrix4x4 InterpolateScale(double time, NodeAnimationChannel channel)
    {
        Vector3D scale;

        if (channel.ScalingKeyCount == 1)
        {
            scale = channel.ScalingKeys[0].Value;
        }
        else
        {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.ScalingKeyCount - 1; i++)
            {
                if (time < (float)channel.ScalingKeys[(int)(i + 1)].Time)
                {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.ScalingKeys[(int)frameIndex];
            VectorKey nextFrame = channel.ScalingKeys[(int)((frameIndex + 1) % channel.ScalingKeyCount)];

            double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;

            scale = (start + (float)delta * (end - start));
        }

        return aiMatrix4x4.FromScaling(scale);
    }
}

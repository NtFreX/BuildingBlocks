using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh;

//  TODO: other animation providers
public interface IBoneAnimationProvider
{
    Matrix4x4[] Transforms { get; }

    bool IsRunning { get; set; }

    void UpdateAnimation(float deltaSeconds);
}

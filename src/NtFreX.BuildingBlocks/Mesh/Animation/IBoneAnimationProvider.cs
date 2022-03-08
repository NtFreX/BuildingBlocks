using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh;

//  TODO: other animation providers
public interface IBoneAnimationProvider
{
    bool IsRunning { get; set; }

    void UpdateAnimation(float deltaSeconds, ref Matrix4x4[] transforms);
}

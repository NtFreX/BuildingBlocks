namespace NtFreX.BuildingBlocks.Model;

// TODO: make dynamic
public enum RenderPasses : int
{
    Forward = 1 << 0,
    AlphaBlend = 1 << 1,
    Overlay = 1 << 2,
    ShadowMapNear = 1 << 3,
    ShadowMapMid = 1 << 4,
    ShadowMapFar = 1 << 5,
    //Duplicator = 1 << 6,
    //SwapchainOutput = 1 << 7,
    ReflectionMap = 1 << 8,
    Particles = 1 << 9,
    Geometry = 1 << 10,
    GeometryAlpha = 1 << 11,
    AllShadowMap = ShadowMapNear | ShadowMapMid | ShadowMapFar,
}


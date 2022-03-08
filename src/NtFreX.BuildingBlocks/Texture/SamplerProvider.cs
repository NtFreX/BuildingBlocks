using Veldrid;

namespace NtFreX.BuildingBlocks.Texture;

public abstract class SamplerProvider
{
    public abstract Sampler Get(GraphicsDevice graphicsDevice);
}

public class PointSamplerProvider : SamplerProvider
{
    public override Sampler Get(GraphicsDevice graphicsDevice) => graphicsDevice.PointSampler;
}

public class LinearSamplerProvider : SamplerProvider
{
    public override Sampler Get(GraphicsDevice graphicsDevice) => graphicsDevice.LinearSampler;
}

public class Aniso4xSamplerProvider : SamplerProvider
{
    public override Sampler Get(GraphicsDevice graphicsDevice) => graphicsDevice.Aniso4xSampler;
}
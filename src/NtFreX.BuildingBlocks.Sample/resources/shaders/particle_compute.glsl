#version 450

struct ParticleInfo
{
    vec3 Position;
    vec3 Scale;
    vec3 Velocity;
    vec4 Color;
};

layout(std140, set = 0, binding = 0) buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

layout(set = 1, binding = 0) uniform ScreenSizeBuffer
{
    uint ParticleCount;
    vec3 Padding_;
};

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

void main()
{
    uint index = gl_GlobalInvocationID.x;
    if (index > ParticleCount)
    {
        return;
    }

    Particles[index].Position = Particles[index].Position + Particles[index].Velocity;
}

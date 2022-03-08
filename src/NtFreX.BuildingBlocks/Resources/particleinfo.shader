
struct ParticleInfo
{
    vec3 Position;
    float Scale;
    vec3 Velocity;
    float _padding0;
    vec4 Color;
    vec3 TexCoords;
    float _padding1;
};

layout(std140, set = 0, binding = 0) buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

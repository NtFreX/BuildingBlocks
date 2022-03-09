
struct ParticleInfo
{
    vec3 Position;
    float Scale;
    vec3 Velocity;
    float LivetimeModifer;
    vec4 Color;
    vec4 ColorModifier;
    vec4 InitialColor;
    vec3 TexCoords;
    float Livetime;
    vec3 InitialVelocity;
    float _padding0;
    vec3 VelocityModifer;
    float _padding1;
};

layout(std140, set = 0, binding = 0) buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

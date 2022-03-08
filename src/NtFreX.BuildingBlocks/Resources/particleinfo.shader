
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
};

layout(std140, set = 0, binding = 0) buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

#version 450

struct ParticleInfo
{
    vec3 Position;
    vec3 Scale;
    vec3 Velocity;
    vec4 Color;
};

layout(std140, set = 0, binding = 0) readonly buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

layout(set = 1, binding = 0) uniform ScreenSizeBuffer
{
    uint ParticleCount;
    vec3 Padding_;
};

layout(set = 2, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 2, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 2, binding = 2) uniform WorldBuffer
{
    mat4 World;
};


layout (location = 0) out vec4 fsin_color;

void main()
{
    mat3 pos = mat3(
        1, 0, Particles[gl_VertexIndex].Position.x,
        0, 1, Particles[gl_VertexIndex].Position.y,
        0, 0, Particles[gl_VertexIndex].Position.z
    );
    mat3 scale = mat3(
        Particles[gl_VertexIndex].Scale.x, 0, 0,
        0, Particles[gl_VertexIndex].Scale.y, 0,
        0, 0, Particles[gl_VertexIndex].Scale.z
    );

    vec4 worldPosition = World * vec4(scale * pos * vec3(0, 0, 0), 1);
    vec4 viewPosition = View * worldPosition;
    vec4 outputPosition = Projection * viewPosition;
    gl_Position = outputPosition;

    //gl_PointSize = Particles[gl_VertexIndex].Scale.x * Particles[gl_VertexIndex].Scale.y * Particles[gl_VertexIndex].Scale.z;
    fsin_color = Particles[gl_VertexIndex].Color;
}

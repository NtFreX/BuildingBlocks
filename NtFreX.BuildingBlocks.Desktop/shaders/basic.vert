#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 0, binding = 2) uniform WorldBuffer
{
    mat4 World;
};

layout(set = 0, binding = 2) uniform LightBuffer
{
    vec4 LightPos;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec4 fsin_color;
layout(location = 1) out vec3 fsin_eyePos;
layout(location = 2) out vec3 fsin_normal;
layout(location = 3) out vec3 fsin_lightVec;

void main()
{
    vec4 pos = vec4(Position, 1.f);

    fsin_normal = Normal;
    fsin_Color = Color;

    gl_Position = Projection * View * World * pos;

    vec4 eyePos = Projection * World * pos;
    fsin_eyePos = eyePos.xyz;
    
    vec4 eyeLightPos = View * LightPos;
    fsin_lightVec = normalize(LightPos.xyz - fsin_eyePos);
}
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

layout(set = 0, binding = 3) uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};


layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TextureCoordinate;
layout(location = 3) in vec4 Color;
layout(location = 4) in vec3 InstancePosition;
layout(location = 5) in vec3 InstanceRotation;
layout(location = 6) in vec3 InstanceScale;
layout(location = 7) in int InstanceTexArrayIndex;

layout(set = 9, location = 0) out vec4 fsin_color;
layout(set = 9, location = 1) out vec3 fsin_texCoords;

void main()
{
    fsin_color = Color;
    fsin_texCoords = vec3(TextureCoordinate, InstanceTexArrayIndex);

    gl_Position = /*World * Projection * */ vec4(Position, 1)/10000000.0f;
}
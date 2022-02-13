#version 450

layout(set = #{worldViewProjectionSet}, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = #{worldViewProjectionSet}, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = #{worldViewProjectionSet}, binding = 2) uniform WorldBuffer
{
    mat4 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TextureCoordinate;
layout(location = 3) in vec4 Color;

#if hasInstances
    layout(location = 4) in vec3 InstancePosition;
    layout(location = 5) in vec3 InstanceRotation;
    layout(location = 6) in vec3 InstanceScale;
    layout(location = 7) in int InstanceTexArrayIndex;
#endif

layout(location = 0) out vec4 fsin_color;
layout(location = 1) out vec2 fsin_texCoords;

#if hasInstances #include ./matrix3x3.shader

void main()
{
    #if hasInstances
        vec3 transformedPos = transform(Position, InstanceScale, InstanceRotation) + InstancePosition;
    #else
        vec3 transformedPos = Position;
    #endif


    fsin_color = Color;
    fsin_texCoords = TextureCoordinate;
    
    vec4 worldPosition = World * vec4(transformedPos, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
}
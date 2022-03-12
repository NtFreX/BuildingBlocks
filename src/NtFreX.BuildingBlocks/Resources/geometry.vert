#version 450

#include ./viewprojectionworldlayout.shader

layout(set = #{inverseWorldSet}, binding = 0) uniform InverseWorldBuffer
{
    mat4 InverseWorld;
};

#include ./bonetransformlayout.shader
#include ./vertexlayout.shader

layout(location = 0) out vec4 fsin_color;
layout(location = 1) out vec3 fsin_texCoords;
layout(location = 2) out vec3 fsin_positionWorldSpace;
layout(location = 3) out vec3 fsin_normal;

#if hasInstances #include ./standard/matrix3x3.shader #endif

void main()
{
    #include ./transformnormal.shader
    #include ./transformposition.shader
    #include ./transformtexcoords.shader

    #if hasColor
        fsin_color = Color;
    #else 
        fsin_color = vec4(0, 0, 0, 1);
    #endif

    fsin_texCoords = transformedTexCoords;

    vec4 worldNormal = InverseWorld * transformedNormal;
    fsin_normal = worldNormal.xyz;

    vec4 worldPosition = World * transformedPos;
    fsin_positionWorldSpace = worldPosition.xyz;

    gl_Position = Projection * View * worldPosition;
}
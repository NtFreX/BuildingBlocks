#version 450

#include ./viewprojectionworldlayout.shader

layout(set = #{inverseWorldSet}, binding = 0) uniform InverseWorldBuffer
{
    mat4 InverseWorld;
};

#if hasReflection
    layout(set = #{reflectionSet}, binding = 3) uniform ReflectionViewProjBuffer
    {
        mat4 ReflectionViewProj;
    };
#endif

#if hasLights
    layout(set = #{shadowVertexSet}, binding = 0) uniform LightProjectionNearBuffer
    {
        mat4 LightProjectionNear;
    };
    layout(set = #{shadowVertexSet}, binding = 1) uniform LightViewNearBuffer
    {
        mat4 LightViewNear;
    };
    layout(set = #{shadowVertexSet}, binding = 2) uniform LightProjectionMidBuffer
    {
        mat4 LightProjectionMid;
    };
    layout(set = #{shadowVertexSet}, binding = 3) uniform LightViewMidBuffer
    {
        mat4 LightViewMid;
    };
    layout(set = #{shadowVertexSet}, binding = 4) uniform LightProjectionFarBuffer
    {
        mat4 LightProjectionFar;
    };
    layout(set = #{shadowVertexSet}, binding = 5) uniform LightViewFarBuffer
    {
        mat4 LightViewFar;
    };
#endif

#include ./bonetransformlayout.shader
#include ./vertexlayout.shader

layout(location = 0) out vec4 fsin_color;
layout(location = 1) out vec3 fsin_texCoords;
layout(location = 2) out vec3 fsin_positionWorldSpace;
layout(location = 3) out vec4 fsin_reflectionPosition;
layout(location = 4) out vec3 fsin_normal;
#if hasLights 
    layout(location = 5) out float fsin_fragDepth;
    layout(location = 6) out vec4 fsin_lightPositionNear;
    layout(location = 7) out vec4 fsin_lightPositionMid;
    layout(location = 8) out vec4 fsin_lightPositionFar;
 #endif

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

    #if hasReflection fsin_reflectionPosition = worldPosition * ReflectionViewProj; #endif

    gl_Position = Projection * View * worldPosition;
    
    #if hasLights 
        fsin_lightPositionNear = LightProjectionNear * LightViewNear * worldPosition;
        fsin_lightPositionMid = LightProjectionMid * LightViewMid * worldPosition;
        fsin_lightPositionFar = LightProjectionFar * LightViewFar * worldPosition;
        fsin_fragDepth = gl_Position.z;
    #endif
}
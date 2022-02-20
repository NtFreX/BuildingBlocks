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

#if hasReflection
    layout(set = #{reflectionSet}, binding = 3) uniform ReflectionViewProjBuffer
    {
        mat4 ReflectionViewProj;
    };
#endif

#if hasBones
    layout(set = #{boneTransformationsSet}, binding = 0) uniform BonesBuffer
    {
        mat4 BonesTransformations[#{maxBoneTransforms}];
    };
#endif

#if isPositionNormalTextureCoordinateColor
    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Normal;
    layout(location = 2) in vec2 TextureCoordinate;
    layout(location = 3) in vec4 Color;
#elseif isPositionNormalTextureCoordinate
    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Normal;
    layout(location = 2) in vec2 TextureCoordinate;
#elseif isPositionNormal
    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Normal;
#elseif isPosition
    layout(location = 0) in vec3 Position;
#endif

#if hasBones
    layout(location = #{boneWeightsLocation}) in vec4 BoneWeights;
    layout(location = #{boneIndicesLocation}) in uvec4 BoneIndices;
#endif

#if hasInstances
    layout(location = #{instancePositionLocation}) in vec3 InstancePosition;
    layout(location = #{instanceRotationLocation}) in vec3 InstanceRotation;
    layout(location = #{instanceScaleLocation}) in vec3 InstanceScale;
    layout(location = #{instanceTexArrayIndexLocation}) in int InstanceTexArrayIndex;
#endif

layout(location = 0) out vec4 fsin_color;
layout(location = 1) out vec3 fsin_texCoords;
layout(location = 2) out vec3 fsin_positionWorldSpace;
layout(location = 3) out vec4 fsin_reflectionPosition;
#if hasNormal layout(location = 4) out vec3 fsin_normal; #endif

#if hasInstances #include ./matrix3x3.shader #endif

void main()
{
    #if hasInstances
        vec4 transformedPos = vec4(matrix3x3Transform(Position, InstanceScale, InstanceRotation) + InstancePosition, 1);
    #else
        vec4 transformedPos = vec4(Position, 1);
    #endif
    
    #if hasBones
        mat4 boneTransformation = BonesTransformations[BoneIndices.x] * BoneWeights.x;
        boneTransformation += BonesTransformations[BoneIndices.y] * BoneWeights.y;
        boneTransformation += BonesTransformations[BoneIndices.z] * BoneWeights.z;
        boneTransformation += BonesTransformations[BoneIndices.w] * BoneWeights.w;
        transformedPos = boneTransformation * transformedPos;
    #endif

    #if hasColor
        fsin_color = Color;
    #else 
        fsin_color = vec4(0, 0, 0, 1);
    #endif

    #if hasTextureCoordinate
        #if hasInstances
            fsin_texCoords = vec3(TextureCoordinate, InstanceTexArrayIndex);
        #else
            fsin_texCoords = vec3(TextureCoordinate, 0);
        #endif
    #else 
        fsin_texCoords = vec3(0, 0, 0);
    #endif

    #if hasNormal fsin_normal = Normal; #endif
    
    vec4 worldPosition = World * transformedPos;
    fsin_positionWorldSpace = worldPosition.xyz;

    #if hasReflection fsin_reflectionPosition = worldPosition * ReflectionViewProj; #endif

    gl_Position = Projection * View * worldPosition;
}
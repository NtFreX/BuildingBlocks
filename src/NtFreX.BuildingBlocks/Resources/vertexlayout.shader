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
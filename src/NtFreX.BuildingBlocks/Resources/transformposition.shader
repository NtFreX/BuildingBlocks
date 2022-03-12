#include ./bonetransform.shader

vec4 transformedPos = boneTransformation * vec4(Position, 1);

#if hasInstances
    transformedPos = vec4(matrix3x3Transform(transformedPos.xyz, InstanceScale, InstanceRotation) + InstancePosition, 1);    
#endif
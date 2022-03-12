#include ./bonetransform.shader

#if hasNormal
    vec4 transformedNormal = normalize(vec4(Normal, 1));
#else
    vec4 transformedNormal = vec4(0, 0, 0, 0);
#endif

#if hasBones
    transformedNormal = boneTransformation * transformedNormal;
#endif 

#if hasInstances
    transformedNormal = vec4(matrix3x3CreateRotationMatrix(InstanceRotation) * transformedNormal.xyz, 0);    
#endif
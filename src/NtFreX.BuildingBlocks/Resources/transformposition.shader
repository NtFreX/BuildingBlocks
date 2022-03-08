#if hasBones
    mat4 boneTransformation = BonesTransformations[BoneIndices.x] * BoneWeights.x;
    boneTransformation += BonesTransformations[BoneIndices.y] * BoneWeights.y;
    boneTransformation += BonesTransformations[BoneIndices.z] * BoneWeights.z;
    boneTransformation += BonesTransformations[BoneIndices.w] * BoneWeights.w;
    vec4 transformedPos = boneTransformation * vec4(Position, 1);
#else
    vec4 transformedPos = vec4(Position, 1);
#endif

#if hasInstances
    transformedPos = vec4(matrix3x3Transform(transformedPos.xyz, InstanceScale, InstanceRotation) + InstancePosition, 1);    
#endif
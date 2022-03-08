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
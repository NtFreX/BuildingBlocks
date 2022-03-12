#if hasBones
    mat4 boneTransformation = BonesTransformations[BoneIndices.x] * BoneWeights.x;
    boneTransformation += BonesTransformations[BoneIndices.y] * BoneWeights.y;
    boneTransformation += BonesTransformations[BoneIndices.z] * BoneWeights.z;
    boneTransformation += BonesTransformations[BoneIndices.w] * BoneWeights.w;
#else
    mat4 boneTransformation = mat4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);
#endif
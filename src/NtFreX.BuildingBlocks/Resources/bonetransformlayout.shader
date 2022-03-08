#if hasBones
    layout(set = #{boneTransformationsSet}, binding = 0) uniform BonesBuffer
    {
        mat4 BonesTransformations[#{maxBoneTransforms}];
    };
#endif
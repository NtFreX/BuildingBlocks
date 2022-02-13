mat3 createRotationMatrix(vec3 rotation) 
{
    float cosX = cos(rotation.x);
    float sinX = sin(rotation.x);
    mat3 rotX = mat3(
        1, 0, 0,
        0, cosX, -sinX,
        0, sinX, cosX);

    float cosY = cos(rotation.y);
    float sinY = sin(rotation.y);
    mat3 rotY = mat3(
        cosY, 0, sinY,
        0, 1, 0,
        -sinY, 0, cosY);

    float cosZ = cos(rotation.z);
    float sinZ = sin(rotation.z);
    mat3 rotZ =mat3(
        cosZ, -sinZ, 0,
        sinZ, cosZ, 0,
        0, 0, 1);

    return rotX * rotY * rotZ;
}

mat3 createScalingMatrix(vec3 scale)
{
    return mat3(scale.x, 0, 0, 0, scale.y, 0, 0, 0, scale.z);
}

vec3 transform(vec3 position, vec3 scale, vec3 rotation)
{
    return createScalingMatrix(scale) * createRotationMatrix(rotation) * position;
}
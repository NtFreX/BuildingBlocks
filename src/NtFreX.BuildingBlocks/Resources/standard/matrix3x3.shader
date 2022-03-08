mat3 matrix3x3CreateRotationMatrix(vec3 rotation) 
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

mat3 matrix3x3CreateScalingMatrix(vec3 scale)
{
    return mat3(scale.x, 0, 0, 0, scale.y, 0, 0, 0, scale.z);
}

vec3 matrix3x3Transform(vec3 position, vec3 scale, vec3 rotation)
{
    return matrix3x3CreateScalingMatrix(scale) * matrix3x3CreateRotationMatrix(rotation) * position;
}

mat3 matrix3x3Identity()
{
    return mat3(1,0,0, 0,1,0, 0,0,1);
}

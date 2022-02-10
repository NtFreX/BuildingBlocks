#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 0, binding = 2) uniform WorldBuffer
{
    mat4 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TextureCoordinate;
layout(location = 3) in vec4 Color;
layout(location = 4) in vec3 InstancePosition;
layout(location = 5) in vec3 InstanceRotation;
layout(location = 6) in vec3 InstanceScale;
layout(location = 7) in int InstanceTexArrayIndex;

layout(set = 9, location = 0) out vec3 fsin_position_worldSpace;
layout(set = 9, location = 1) out vec4 fsin_lightPosition;
layout(set = 9, location = 2) out vec3 fsin_normal;
layout(set = 9, location = 3) out vec4 fsin_color;
layout(set = 9, location = 4) out vec3 fsin_texCoords;
layout(set = 9, location = 5) out float fsin_fragCoord;

void main()
{
    float cosX = cos(InstanceRotation.x);
    float sinX = sin(InstanceRotation.x);
    mat3 instanceRotX = mat3(
        1, 0, 0,
        0, cosX, -sinX,
        0, sinX, cosX);

    float cosY = cos(InstanceRotation.y);
    float sinY = sin(InstanceRotation.y);
    mat3 instanceRotY = mat3(
        cosY, 0, sinY,
        0, 1, 0,
        -sinY, 0, cosY);

    float cosZ = cos(InstanceRotation.z);
    float sinZ = sin(InstanceRotation.z);
    mat3 instanceRotZ =mat3(
        cosZ, -sinZ, 0,
        sinZ, cosZ, 0,
        0, 0, 1);

    mat3 instanceRotFull = instanceRotZ * instanceRotY * instanceRotZ;
    mat3 scalingMat = mat3(InstanceScale.x, 0, 0, 0, InstanceScale.y, 0, 0, 0, InstanceScale.z);
    

    vec3 transformedPos = (scalingMat * instanceRotFull * Position) + InstancePosition;
        
    fsin_normal = normalize(instanceRotFull * Normal);
    fsin_color = Color;
    fsin_texCoords = vec3(TextureCoordinate, InstanceTexArrayIndex);
        
    vec4 worldPosition = World * vec4(transformedPos, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 outputPosition = Projection * viewPosition;
    gl_Position = outputPosition;
    
    fsin_position_worldSpace = worldPosition.xyz;

    fsin_lightPosition = World * vec4(transformedPos, 1);
    fsin_lightPosition = View * fsin_lightPosition;
    fsin_lightPosition = Projection * fsin_lightPosition;

    fsin_fragCoord = outputPosition.z;

}
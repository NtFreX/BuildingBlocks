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
layout(location = 2) in vec2 TextureCoordinates;
layout(location = 3) in vec4 Color;

layout(set = 9, location = 0) out vec3 fsin_position_worldSpace;
layout(set = 9, location = 1) out vec4 fsin_lightPosition;
layout(set = 9, location = 2) out vec3 fsin_normal;
layout(set = 9, location = 3) out vec4 fsin_color;
layout(set = 9, location = 4) out vec2 fsin_texCoords;
layout(set = 9, location = 5) out float fsin_fragCoord;

void main()
{
    fsin_normal = Normal;
    fsin_color = Color;
    fsin_texCoords = TextureCoordinates;
        
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 outputPosition = Projection * viewPosition;
    gl_Position = outputPosition;
    
    fsin_position_worldSpace = worldPosition.xyz;

    fsin_lightPosition = World * vec4(Position, 1);
    fsin_lightPosition = View * fsin_lightPosition;
    fsin_lightPosition = Projection * fsin_lightPosition;

    fsin_fragCoord = outputPosition.z;

}
#version 450

layout(set = 1, binding = 0) uniform Camera
{
    vec3 CameraPosition;
    float CameraNearPlaneDistance;
    vec3 CameraLookDirection;
    float CameraFarPlaneDistance;
};

#define MAX_POINT_LIGHTS 4

struct PointLightInfo
{
    vec3 Position;
    float Range;
    vec3 Color;
    float Intensity;
};

layout(set = 2, binding = 0) uniform Lights
{
    int ActivePointLights;
    vec3 AmbientLight;
    PointLightInfo PointLights[MAX_POINT_LIGHTS];
};

layout(set = 3, binding = 0) uniform Material
{
    float Opacity;
    float Shininess;
    float ShininessStrength;
    float Reflectivity;
    vec4 AmbientColor;
    vec4 DiffuseColor;
    vec4 EmissiveColor;
    vec4 ReflectiveColor;
    vec4 SpecularColor;
    vec4 TransparentColor;
};

layout(set = 4, binding = 0) uniform texture2DArray SurfaceTexture;
layout(set = 4, binding = 1) uniform sampler SurfaceSampler;

layout(set = 9, location = 0) in vec4 fsin_color;
layout(set = 9, location = 1) in vec3 fsin_texCoords;

layout(location=0) out vec4 accumColor;
layout(location=1) out float accumAlpha;

void main()
{
    vec4 surfaceColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_texCoords);
    /*if(surfaceColor.x + surfaceColor.y + surfaceColor.z + surfaceColor.w == 0.0f) {
        discard;
    }*/

    accumColor = fsin_color;
    accumAlpha = fsin_color.a;
    return;
}
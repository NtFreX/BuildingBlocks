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

layout(set = 9, location = 0) in vec3 fsin_position_worldSpace;
layout(set = 9, location = 1) in vec4 fsin_lightPosition;
layout(set = 9, location = 2) in vec3 fsin_normal;
layout(set = 9, location = 3) in vec4 fsin_color;
layout(set = 9, location = 4) in vec3 fsin_texCoords;
layout(set = 9, location = 5) in float fsin_fragCoord;

layout(location=0) out vec4 accumColor;
layout(location=1) out float accumAlpha;

float GetPointLightAttenuation(float distance, float radius, float cutoff)
{
    // Attenuation formula: https://imdoingitwrong.wordpress.com/2011/01/31/light-attenuation/

    // calculate basic attenuation
    float denom = distance / radius + 1;
    float attenuation = 1 / (denom * denom);

    // scale and bias attenuation such that:
    //   attenuation == 0 at extent of max influence
    //   attenuation == 1 when distance == 0
    attenuation = (attenuation - cutoff) / (1 - cutoff);
    attenuation = max(attenuation, 0);

    return attenuation;
}
float weight(float z, float a) {
    return clamp(pow(min(1.0, a * 10.0) + 0.01, 3.0) * 1e8 * pow(1.0 - z * 0.9, 3.0), 1e-2, 3e3);
}
/*
float GetLinearDepth(float depth)
{
    float zNear = CameraNearPlaneDistance;
    float zFar = CameraFarPlaneDistance;
    float depthSample = 2.0 * depth - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - depthSample * (zFar - zNear));
}
*/
void main()
{
    float specularPower = Shininess;
    float specularIntensity = ShininessStrength;
    float attenuationCutoff = 0.001;
    //vec3 directionalLightDir = vec3(0, 0, 0);

    vec4 surfaceColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_texCoords) + fsin_color;
     
    vec4 pointDiffuse = vec4(0, 0, 0, 1);
    vec4 pointSpec = vec4(0, 0, 0, 1);
    for (int i = 0; i < ActivePointLights; i++)
    {
        PointLightInfo pli = PointLights[i];
        vec3 lightDir = normalize(pli.Position - fsin_position_worldSpace);
        float intensity = clamp(dot(fsin_normal, lightDir), 0, 1);
        float lightDistance = distance(pli.Position, fsin_position_worldSpace);
        float attenuation = GetPointLightAttenuation(lightDistance, pli.Range, attenuationCutoff);
        
        vec4 difuseColor = vec4(pli.Color, 1) * attenuation + DiffuseColor * attenuation; //vec3(pli.Color.x + DiffuseColor.x / 2, pli.Color.y + DiffuseColor.y / 2, pli.Color.z + DiffuseColor.z / 2);
        pointDiffuse += intensity * difuseColor * pli.Intensity;

        // Specular
        vec3 vertexToEye = normalize(CameraPosition - fsin_position_worldSpace);
        vec3 lightReflect = -normalize(reflect(lightDir, fsin_normal));

        float specularFactor = dot(vertexToEye, lightReflect);
        if (specularFactor > 0)
        {
            specularFactor = pow(abs(specularFactor), specularPower);
            vec4 specularColor = attenuation * vec4(pli.Color, 1) + attenuation * SpecularColor; //vec3(pli.Color.x + SpecularColor.x / 2, pli.Color.y + SpecularColor.y / 2, pli.Color.z + SpecularColor.z / 2);
            pointSpec += specularColor * specularIntensity * specularFactor;
        }
    }

    //float fragDepth = GetLinearDepth(gl_FragCoord.z);
    //ivec2 sceneTexCoord = ivec2(gl_FragCoord.xy);
    //float depthSample = texelFetch(DepthTexture, sceneTexCoord, 0).r;
    //float sceneDepth = GetLinearDepth(depthSample);
    //float diff = sceneDepth - fragDepth;
    //if (diff < 0)
    //{
    //    discard;
    //}

    //vec3 L = -1 * normalize(directionalLightDir);
    //float diffuseFactor = dot(normalize(fsin_normal), L);
    //diffuseFactor = clamp(diffuseFactor, 0, 1);

    vec4 emissiveAmbient = EmissiveColor + AmbientColor; // vec4(EmissiveColor.x + AmbientColor.x / 2, EmissiveColor.y + AmbientColor.y / 2, EmissiveColor.z + AmbientColor.z / 2, EmissiveColor.w + AmbientColor.w / 2);
    vec4 worldAmbientCombined = vec4(AmbientLight, 1) + emissiveAmbient; // vec4(AmbientLight.x + emissiveAmbient.x / 2, AmbientLight.y + emissiveAmbient.y / 2, AmbientLight.z + emissiveAmbient.z / 2, emissiveAmbient.w)
    vec4 beforeTint = (worldAmbientCombined * surfaceColor /*+ (surfaceColor * diffuseFactor)*/) + pointDiffuse + pointSpec;
    //outputColor = ApplyTintColor(beforeTint, tintColor, tintFactor);
    
    vec4 realColor = vec4(beforeTint.xyz, clamp(Opacity + surfaceColor.w, 0, 1));

    
    float w = weight(gl_FragCoord.z, realColor.a);
    accumColor = vec4(realColor.xyz, realColor.a);
    accumAlpha = realColor.a * w;
    return;
}
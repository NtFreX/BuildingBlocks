#version 450

layout(set = #{cameraInfoSet}, binding = 0) uniform CameraBuffer
{
    vec3 CameraPosition;
    float CameraNearPlaneDistance;
    vec3 CameraLookDirection;
    float CameraFarPlaneDistance;
};

#if hasTexture
    layout(set = #{textureSet}, binding = 0) uniform texture2DArray SurfaceTexture;
    layout(set = #{textureSet}, binding = 1) uniform sampler SurfaceSampler;
#endif
#if hasAlphaMap
    layout(set = #{alphaMapSet}, binding = 0) uniform texture2DArray AlphaMap;
    layout(set = #{alphaMapSet}, binding = 1) uniform sampler AlphaMapSampler;
#endif
#if hasReflection
    layout(set = #{reflectionSet}, binding = 0) uniform texture2DArray ReflectionMap;
    layout(set = #{reflectionSet}, binding = 1) uniform sampler ReflectionSampler;
    layout(set = #{reflectionSet}, binding = 3) uniform ClipInfo
    {
        vec4 ClipPlane;
        int ClipPlaneEnabled;
    };
#endif
#if hasLights
    #define MAX_POINT_LIGHTS #{maxPointLights}

    struct PointLightInfo
    {
        vec3 Position;
        float Range;
        vec3 Color;
        float Intensity; // TODO: use this correctly
    };

    layout(set = #{lightSet}, binding = 0) uniform LightBuffer
    {
        vec3 DirectionalLightDirection;
        float _padding;
        vec4 DirectionalLightColor;

        vec4 AmbientLight;

        int ActivePointLights;
        vec3 _padding1;

        PointLightInfo PointLights[MAX_POINT_LIGHTS];
    };
#endif

#if hasMaterial
    layout(set = #{materialSet}, binding = 0) uniform MaterialBuffer
    {
        float Opacity;
        float Shininess;
        float ShininessStrength;
        float Reflectivity;
        vec4 AmbientColor; 
        vec4 DiffuseColor;
        vec4 EmissiveColor; // TODO: use this value (boom render pass)
        vec4 ReflectiveColor;
        vec4 SpecularColor;
        vec4 TransparentColor; // TODO: use this value or delete it
    };
#endif

layout(set = 1, location = 0) in vec4 fsin_color;
layout(set = 1, location = 1) in vec3 fsin_texCoords;
layout(set = 1, location = 2) in vec3 fsin_positionWorldSpace;
layout(set = 1, location = 3) in vec4 fsin_reflectionPosition;
#if hasNormal layout(set = 1, location = 4) in vec3 fsin_normal; #endif

layout(location=0) out vec4 accumColor;
layout(location=1) out float accumAlpha;

// TODO: use those
layout(constant_id = 100) const bool ClipSpaceInvertedY = true;
layout(constant_id = 101) const bool TextureCoordinatesInvertedY = false;
layout(constant_id = 102) const bool ReverseDepthRange = true;

vec2 ClipToUV(vec4 clip)
{
    vec2 ret = vec2((clip.x / clip.w) / 2 + 0.5, (clip.y / clip.w) / -2 + 0.5);
    if (ClipSpaceInvertedY || TextureCoordinatesInvertedY)
    {
        ret.y = 1 - ret.y;
    }

    return ret;
}

void main()
{    
    #if hasReflection
        if (ClipPlaneEnabled == 1)
        {
            if (dot(ClipPlane, vec4(fsin_positionWorldSpace, 1)) < 0)
            {
                discard;
            }
        }
    #endif

    #if hasAlphaMap
        float alphaMapSample = texture(sampler2DArray(AlphaMap, AlphaMapSampler), fsin_texCoords).x;
        if (alphaMapSample == 0)
        {
            discard;
        }
    #endif

    vec4 color = fsin_color;
    
    #if hasTexture
        vec4 textureColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_texCoords);
        color = color + textureColor;
    #endif
    
    
    #if hasNormal 
        vec3 normal = fsin_normal;
    #else 
        vec3 normal = vec3(0, 0, 0);
    #endif
        
    vec3 vertexToEye = normalize(fsin_positionWorldSpace - CameraPosition);
    vec4 directionalSpecColor = vec4(0, 0, 0, 0);
    #if hasMaterial
        float shininess = Shininess;
        float shininessStrength = ShininessStrength;
        vec4 diffuseColor = DiffuseColor;
        vec4 ambientColor = AmbientColor;

        if (Reflectivity > 0)
        {
            #if hasReflection
                vec2 reflectionTexCoords = ClipToUV(fsin_reflectionPosition);
                vec4 reflectionSample = texture(sampler2DArray(ReflectionMap, ReflectionSampler), fsin_reflectionPosition);
                color = (color * (1 - Reflectivity)) + (reflectionSample * Reflectivity);
            #else
                color = (color * (1 - Reflectivity)) + (ReflectiveColor * Reflectivity);
            #endif
        }

        if(shininess > 0) 
        {
            #if hasLights
                vec3 lightReflect = normalize(reflect(DirectionalLightDirection, normal));
                float specularFactor = dot(vertexToEye, lightReflect);
                if (specularFactor > 0)
                {
                    specularFactor = pow(abs(specularFactor), shininess);
                    directionalSpecColor = SpecularColor * vec4(DirectionalLightColor.xyz * shininessStrength * specularFactor, 1.0f);
                }
            #else
                directionalSpecColor = SpecularColor * shininessStrength * pow(normal.x + normal.y + normal.z, shininess);
            #endif
        }
    #else 
        float shininess = 0;
        float shininessStrength = 0.2;
        vec4 diffuseColor = vec4(0, 0, 0, 1);
        vec4 ambientColor = vec4(0, 0, 0, 1);
    #endif
    
    vec4 directionalColor = vec4(0, 0, 0, 0);
    #if hasLights
        vec4 ambientLight = ((AmbientLight + ambientColor) / 2) * color;
        float lightIntensity = clamp(dot(normal, DirectionalLightDirection), 0, 1);
        if (lightIntensity > 0.0f)
        {
            directionalColor = color * lightIntensity * DirectionalLightColor;
        }
        
        vec4 pointDiffuse = vec4(0, 0, 0, 1);
        vec4 pointSpec = vec4(0, 0, 0, 1);
        for (int i = 0; i < ActivePointLights; i++)
        {
            PointLightInfo pli = PointLights[i];
            vec3 ptLightDir = normalize(pli.Position - fsin_positionWorldSpace);
            float intensity = clamp(dot(normal, ptLightDir), 0, 1);
            float lightDistance = distance(pli.Position, fsin_positionWorldSpace);
            intensity = clamp(intensity * (1 - (lightDistance / pli.Range)), 0, 1);
            pointDiffuse += intensity * vec4(pli.Color, 1) * color * diffuseColor;
            vec3 lightReflect = normalize(reflect(ptLightDir, normal));
            float specularFactor = dot(vertexToEye, lightReflect);
            if (specularFactor > 0 && pli.Range > lightDistance)
            {
                specularFactor = pow(abs(specularFactor), shininess);
                pointSpec += (1 - (lightDistance / pli.Range)) * (vec4(pli.Color * shininessStrength * specularFactor, 1.0f));
            }
        }
    #else
        vec4 ambientLight = ambientColor * color;
        vec4 pointDiffuse = diffuseColor;
        vec4 pointSpec = vec4(0, 0, 0, 0);
    #endif

    color = ambientLight + directionalSpecColor + directionalColor + pointSpec + pointDiffuse;

    #if hasAlphaMap color = vec4(color.xyz, alphaMapSample); #endif
    #if hasMaterial color = vec4(color.xyz, min(color.a, 1) - (1 - Opacity)); #endif

    accumColor = color;
    accumAlpha = accumColor.w;

    if(accumAlpha == 0)
    {
        discard;
    }
}
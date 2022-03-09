#version 450

#include ./cameralayout.shader

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
        float _padding0;

        vec4 Color;

        float Range;
        float _padding2;
        float _padding3;
        float _padding4;

        float Intensity; // TODO: use this correctly
        float _padding5;
        float _padding6;
        float _padding7;
    };

    layout(set = #{lightSet}, binding = 0) uniform DirectionalLightBuffer
    {
        vec4 AmbientLight;
        vec4 DirectionalLightColor;
        vec3 DirectionalLightDirection;
        float _DirectionalLightBuffer_padding0;
    };

    layout(set = #{lightSet}, binding = 1) uniform PointLightCollectionBuffer
    {
        PointLightInfo PointLights[MAX_POINT_LIGHTS];
        int ActivePointLights;
        float _PointLightCollectionBuffer_padding0;
        float _PointLightCollectionBuffer_padding1;
        float _PointLightCollectionBuffer_padding2;
    };

    layout(set = #{shadowSet}, binding = 6) uniform ShadowBuffer
    {
        float NearLimit;
        float MidLimit;
        float FarLimit;
        float _ShadowBuffer_padding_0;
    };

    layout(set = #{shadowSet}, binding = 7) uniform texture2D ShadowMapNear;
    layout(set = #{shadowSet}, binding = 8) uniform texture2D ShadowMapMid;
    layout(set = #{shadowSet}, binding = 9) uniform texture2D ShadowMapFar;
    layout(set = #{shadowSet}, binding = 10) uniform sampler ShadowMapSampler;
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
layout(set = 1, location = 4) in vec3 fsin_normal;
#if hasLights
    layout(set = 1, location = 5) in float fsin_fragDepth;
    layout(set = 1, location = 6) in vec4 fsin_lightPositionNear;
    layout(set = 1, location = 7) in vec4 fsin_lightPositionMid;
    layout(set = 1, location = 8) in vec4 fsin_lightPositionFar;
#endif

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

#if hasLights
    bool IsDepthNearer(float a, float b)
    {
        if (ReverseDepthRange) { return a > b; }
        else { return a < b; }
    }

    bool InRange(float val, float min, float max)
    {
        return val >= min && val <= max;
    }

    float SampleDepthMap(int index, vec2 coord)
    {
        if (index == 0)
        {
            return texture(sampler2D(ShadowMapNear, ShadowMapSampler), coord).x;
        }
        else if (index == 1)
        {
            return texture(sampler2D(ShadowMapMid, ShadowMapSampler), coord).x;
        }
        else
        {
            return texture(sampler2D(ShadowMapFar, ShadowMapSampler), coord).x;
        }
    }

    vec4 GetDirectionalColor(vec4 directionalSpecColor, vec4 ambientLight, vec4 surfaceColor, vec4 diffuseColor) 
    {
        int shadowIndex = 3;
        //TODO: this is the wrong way arround and leads to strange things (or is it?)
        float lightIntensity = clamp(dot(fsin_normal, -DirectionalLightDirection), 0, 1);
        float lightDepthValue = 0;
        vec2 shadowCoords = vec2(0, 0);
        vec2 shadowCoords_near = ClipToUV(fsin_lightPositionNear);
        vec2 shadowCoords_mid = ClipToUV(fsin_lightPositionMid);
        vec2 shadowCoords_far = ClipToUV(fsin_lightPositionFar);
        float lightDepthValues_near = fsin_lightPositionNear.z / fsin_lightPositionNear.w;
        float lightDepthValues_mid = fsin_lightPositionMid.z / fsin_lightPositionMid.w;
        float lightDepthValues_far = fsin_lightPositionFar.z / fsin_lightPositionFar.w;
        
        //TODO: move constances back
        float shadowBias = 0.0005f;
        if (ReverseDepthRange)
        {
            shadowBias *= -1;
        }
        
        if (IsDepthNearer(fsin_fragDepth, NearLimit) && InRange(shadowCoords_near.x, 0, 1) && InRange(shadowCoords_near.y, 0, 1))
        {
            shadowIndex = 0;
            shadowCoords = shadowCoords_near;
            lightDepthValue = lightDepthValues_near;
        }
        else if (IsDepthNearer(fsin_fragDepth, MidLimit) && InRange(shadowCoords_mid.x, 0, 1) && InRange(shadowCoords_mid.y, 0, 1))
        {
            shadowIndex = 1;
            shadowCoords = shadowCoords_mid;
            lightDepthValue = lightDepthValues_mid;
        }
        else if (IsDepthNearer(fsin_fragDepth, FarLimit) && InRange(shadowCoords_far.x, 0, 1) && InRange(shadowCoords_far.y, 0, 1))
        {
            shadowIndex = 2;
            shadowCoords = shadowCoords_far;
            lightDepthValue = lightDepthValues_far;
        }
        
        if (shadowIndex == 3)
        {
            if (lightIntensity > 0.0f)
            {
                return directionalSpecColor + (lightIntensity * surfaceColor * DirectionalLightColor) + (lightIntensity * surfaceColor * diffuseColor) + (ambientLight * surfaceColor);
            }
        }
        else
        {
            float shadowMapDepth = SampleDepthMap(shadowIndex, shadowCoords);
            float biasedDistToLight = (lightDepthValue - shadowBias);
            if (IsDepthNearer(biasedDistToLight, shadowMapDepth))
            {
                if (lightIntensity > 0.0f)
                {
                    return directionalSpecColor + (lightIntensity * surfaceColor * DirectionalLightColor) + (lightIntensity * surfaceColor * diffuseColor) + (ambientLight * surfaceColor);
                }
            }
            else
            {
                return ambientLight * surfaceColor;
            }
        }
        return directionalSpecColor + (ambientLight * surfaceColor);
    }

    vec4 GetPointColor(vec4 surfaceColor, vec4 diffuseColor, vec3 vertexToEye, float shininess, float shininessStrength)
    {
        vec4 pointDiffuse = vec4(0, 0, 0, 1);
        vec4 pointSpec = vec4(0, 0, 0, 1);
        for (int i = 0; i < ActivePointLights; i++)
        {
            PointLightInfo pli = PointLights[i];
            vec3 ptLightDir = normalize(pli.Position - fsin_positionWorldSpace);
            float intensity = clamp(dot(fsin_normal, ptLightDir), 0, 1);
            float lightDistance = distance(pli.Position, fsin_positionWorldSpace);
            intensity = clamp(intensity * (1 - (lightDistance / pli.Range)), 0, 1);
            pointDiffuse += (intensity * pli.Color * surfaceColor) + (intensity * pli.Color * diffuseColor);
            vec3 lightReflect = normalize(reflect(ptLightDir, fsin_normal));
            float specularFactor = dot(vertexToEye, lightReflect);
            if (specularFactor > 0 && pli.Range > lightDistance)
            {
                // TODO: use material spec color?
                specularFactor = pow(abs(specularFactor), shininess);
                pointSpec += (1 - (lightDistance / pli.Range)) * (pli.Color * shininessStrength * specularFactor);
            }
        }
        return pointDiffuse + pointSpec;
    }
#endif

void main()
{    
    #if hasReflection
        if (ClipPlaneEnabled == 1)
        {
            if (dot(ClipPlane, vec4(fsin_positionWorldSpace, 1)) < 0)
            {
                discard;
                return;
            }
        }
    #endif

    #if hasAlphaMap
        float alphaMapSample = texture(sampler2DArray(AlphaMap, AlphaMapSampler), fsin_texCoords).x;
        if (alphaMapSample == 0)
        {
            discard;
            return;
        }
    #endif

    vec4 color = fsin_color;
    
    #if hasTexture
        vec4 textureColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_texCoords);
        color = color + textureColor;
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
                vec3 lightReflect = normalize(reflect(DirectionalLightDirection, fsin_normal));
                float specularFactor = dot(vertexToEye, lightReflect);
                if (specularFactor > 0)
                {
                    specularFactor = pow(abs(specularFactor), shininess);
                    directionalSpecColor = vec4(SpecularColor.xyz  * shininessStrength * specularFactor, 1.0f) + vec4(DirectionalLightColor.xyz * shininessStrength * specularFactor, 1.0f);
                }
            #else
                directionalSpecColor = SpecularColor * shininessStrength * pow(fsin_normal.x + fsin_normal.y + fsin_normal.z, shininess);
            #endif
        }
    #else 
        float shininess = 0;
        float shininessStrength = 0.2;
        vec4 diffuseColor = vec4(0, 0, 0, 1);
        vec4 ambientColor = vec4(0, 0, 0, 1);
    #endif
    
    #if hasLights
        vec4 ambientLight = AmbientLight + ambientColor;
        vec4 directionalColor = GetDirectionalColor(directionalSpecColor, ambientLight, color, diffuseColor);
        vec4 pointColor = GetPointColor(color, diffuseColor, vertexToEye, shininess, shininessStrength);
    #else
        vec4 directionalColor = ambientColor + diffuseColor;
        vec4 pointColor = vec4(0, 0, 0, 1);
    #endif

    color = directionalColor + pointColor;

    #if hasAlphaMap color = vec4(color.xyz, alphaMapSample); #endif
    #if hasMaterial color = vec4(color.xyz, min(color.a, 1) - (1 - Opacity)); #endif

    accumColor = color;
    accumAlpha = accumColor.w;

    if(accumAlpha == 0)
    {
        discard;
        return;
    }
}

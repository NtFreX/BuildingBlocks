#version 450

#if hasTexture
    layout(set = #{textureSet}, binding = 0) uniform texture2D SurfaceTexture;
    layout(set = #{textureSet}, binding = 1) uniform sampler SurfaceSampler;
#endif
#if hasAlphaMap
    layout(set = #{alphaMapSet}, binding = 4) uniform texture2D AlphaMap;
    layout(set = #{alphaMapSet}, binding = 5) uniform sampler AlphaMapSampler;
#endif

layout(set = 1, location = 0) in vec4 fsin_color;
layout(set = 1, location = 1) in vec2 fsin_texCoords;

layout(location=0) out vec4 accumColor;
layout(location=1) out float accumAlpha;

// TODO: use those
layout(constant_id = 100) const bool ClipSpaceInvertedY = true;
layout(constant_id = 101) const bool TextureCoordinatesInvertedY = false;
layout(constant_id = 102) const bool ReverseDepthRange = true;

#include ./color4.shader

void main()
{
    #if hasAlphaMap
        float alphaMapSample = texture(sampler2D(AlphaMap, AlphaMapSampler), fsin_texCoords).x;
        if (alphaMapSample == 0)
        {
            discard;
        }
    #endif

    vec4 color = vec4(0, 0, 0, 1);
    
    #if hasTexture
        vec4 textureColor = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
        color = color4Combine(color, textureColor);
    #endif

    #if hasAlphaMap color = vec4(color.xyz, alphaMapSample);

    accumColor = color4Combine(color, fsin_color);
    accumAlpha = accumColor.w;

    if(accumAlpha == 0)
    {
        discard;
    }
}
#version 450

#if hasTexture
    layout(set = #{textureSet}, binding = 0) uniform texture2DArray SurfaceTexture;
    layout(set = #{textureSet}, binding = 1) uniform sampler SurfaceSampler;
#endif
#if hasAlphaMap
    layout(set = #{alphaMapSet}, binding = 0) uniform texture2DArray AlphaMap;
    layout(set = #{alphaMapSet}, binding = 1) uniform sampler AlphaMapSampler;
#endif
#if hasNormalMap
    layout(set = #{normalMapSet}, binding = 0) uniform texture2DArray NormalMap;
    layout(set = #{normalMapSet}, binding = 1) uniform sampler NormalMapSampler;
#endif
#if hasSpecularMap
    layout(set = #{specularMapSet}, binding = 0) uniform texture2DArray SpecularMap;
    layout(set = #{specularMapSet}, binding = 1) uniform sampler SpecularMapSampler;
#endif

#if hasMaterial
    layout(set = #{materialSet}, binding = 0) uniform MaterialBuffer
    {
        float Opacity; //TODO: use
        float Shininess; //TODO: use
        float ShininessStrength; //TODO: use
        float Reflectivity;//TODO: use

        vec4 AmbientColor; //TODO: use
        vec4 DiffuseColor;
        vec4 EmissiveColor; //TODO: use
        vec4 ReflectiveColor; //TODO: use
        vec4 SpecularColor;
        vec4 TransparentColor; // TODO: use this value or delete it
    };
#endif

layout(set = 1, location = 0) in vec4 fsin_color;
layout(set = 1, location = 1) in vec3 fsin_texCoords;
layout(set = 1, location = 2) in vec3 fsin_positionWorldSpace;
layout(set = 1, location = 3) in vec3 fsin_normal;

layout(location=0) out vec4 gAlbedo;
layout(location=1) out vec4 gfragCord; //TODO can we use the depth buffer instead to help with transparency?
layout(location=2) out vec4 gnormalSpec;

void main()
{
    gfragCord = vec4(fsin_positionWorldSpace, 1);

    #if hasAlphaMap
        float alpha = texture(sampler2DArray(AlphaMap, AlphaMapSampler), fsin_texCoords).x;
    #elseif hasMaterial
        float alpha = Opacity;
    #else
        float alpha = fsin_color.w;
    #endif

    if (alpha == 0)
    {
        discard;
        return;
    }
    
    #if hasTexture
        vec4 textureColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_texCoords);
        gAlbedo = vec4(fsin_color.xyz + textureColor.xyz, alpha);
    #elseif hasMaterial
        gAlbedo = vec4(DiffuseColor.xyz, alpha);
    #else 
        gAlbedo = vec4(fsin_color.xyz, alpha);
    #endif

    #if hasNormalMap
        //TODO: transform normal?
        vec4 normal = texture(sampler2DArray(NormalMap, NormalMapSampler), fsin_texCoords);
        gnormalSpec = vec4(normal.xyz, 1);
    #else
        gnormalSpec = vec4(fsin_normal, 1);
    #endif
    
    #if hasSpecularMap
        vec4 specularColor = texture(sampler2DArray(SpecularMap, SpecularMapSampler), fsin_texCoords);
        gnormalSpec = vec4(gnormalSpec.xyz, specularColor.x);
    #elseif hasMaterial
        gnormalSpec = vec4(gnormalSpec.xyz, SpecularColor.x);
    #else 
        gnormalSpec = vec4(gnormalSpec.xyz, 0);
    #endif
}

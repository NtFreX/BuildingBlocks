#version 450

#if hasTexture
    layout(set = #{textureSet}, binding = 0) uniform texture2D SurfaceTexture;
    layout(set = #{textureSet}, binding = 1) uniform sampler SurfaceSampler;
#endif

layout(set = 1, location = 0) in vec4 fsin_color;
layout(set = 1, location = 1) in vec2 fsin_texCoords;

layout(location=0) out vec4 accumColor;

void main()
{
    vec4 color = vec4(0, 0, 0, 0);
    #if hasTexture color = color + texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
    accumColor = color + fsin_color;
}
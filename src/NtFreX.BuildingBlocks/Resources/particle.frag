#version 450

#if hasTexture
    layout(set = 4, binding = 0) uniform texture2DArray SurfaceTexture;
    layout(set = 4, binding = 1) uniform sampler SurfaceSampler;
#endif

layout (location = 0) in vec4 fsin_color;
layout (location = 1) in vec3 fsin_textCoords;
layout (location = 0) out vec4 fsout_color;
layout (location = 1) out float accumAlpha;

void main()
{
    #if hasTexture
        vec4 textureColor = texture(sampler2DArray(SurfaceTexture, SurfaceSampler), fsin_textCoords);
        fsout_color = textureColor + fsin_color;
    #else
        fsout_color = fsin_color;
    #endif

    accumAlpha = fsout_color.w;
    if(accumAlpha == 0)
    {
        discard;
    }
}

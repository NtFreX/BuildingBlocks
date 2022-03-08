#version 450

#include ./particleinfo.shader
#include ./viewprojectionworldlayout.shader
#include ./cameralayout.shader

layout (location = 0) out vec4 fsin_color;
layout (location = 1) out vec3 fsin_textCoords;

void main()
{
    ParticleInfo particle = Particles[gl_VertexIndex];

    float pointsize = particle.Scale;
    vec3 position = particle.Position;
    gl_PointSize = max(0.01f, pointsize - (distance(CameraPosition, position) / pointsize) / 5); //TODO: make this better
    gl_Position = Projection * View * (World * vec4(position, 1));
    
    fsin_color = particle.Color;
    fsin_textCoords = particle.TexCoords;
}

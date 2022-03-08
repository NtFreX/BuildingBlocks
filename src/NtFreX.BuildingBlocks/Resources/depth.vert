#version 450

#include ./viewprojectionworldlayout.shader
#include ./bonetransformlayout.shader
#include ./vertexlayout.shader

#if hasInstances #include ./matrix3x3.shader #endif

void main()
{
    #include ./transformposition.shader
    #include ./transformtexcoords.shader

    gl_Position = Projection * View * (World * transformedPos);
    gl_Position.y += transformedTexCoords.y * .0001f;
}

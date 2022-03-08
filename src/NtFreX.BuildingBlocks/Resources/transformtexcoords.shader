#if hasTextureCoordinate
    #if hasInstances
        vec3 transformedTexCoords = vec3(TextureCoordinate, InstanceTexArrayIndex);
    #else
        vec3 transformedTexCoords = vec3(TextureCoordinate, 0);
    #endif
#else 
    vec3 transformedTexCoords = vec3(0, 0, 0);
#endif
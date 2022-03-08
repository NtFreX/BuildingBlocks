layout(set = #{cameraInfoSet}, binding = 0) uniform CameraBuffer
{
    vec3 CameraPosition;
    float _padding11;
    
    float CameraNearPlaneDistance;
    float _padding12;
    float _padding13;
    float _padding14;

    vec3 CameraLookDirection;    
    float _padding15;

    float CameraFarPlaneDistance;
    float _padding16;
    float _padding17;
    float _padding18;
};

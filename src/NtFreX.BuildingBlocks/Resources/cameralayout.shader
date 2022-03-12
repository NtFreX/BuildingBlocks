layout(set = #{cameraInfoSet}, binding = 0) uniform CameraBuffer
{
    vec3 CameraPosition;
    float CameraNearPlaneDistance;

    vec3 CameraLookDirection;
    float CameraFarPlaneDistance;
};

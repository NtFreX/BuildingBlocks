layout(set = #{viewProjectionSet}, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = #{viewProjectionSet}, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = #{worldSet}, binding = 0) uniform WorldBuffer
{
    mat4 World;
};
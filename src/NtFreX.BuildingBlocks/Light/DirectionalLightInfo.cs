using System.Numerics;

namespace NtFreX.BuildingBlocks.Light
{
    internal struct DirectionalLightInfo
    {
        public Vector4 AmbientLight;
        public Vector4 DirectionalLightColor;
        public Vector3 DirectionalLightDirection;
        private float _padding = 0;

        public DirectionalLightInfo()
        {
            AmbientLight = new Vector4(.2f, .2f, .2f, 1);
            DirectionalLightDirection = new Vector3(.1f, -.9f, -.1f);
            DirectionalLightColor = new Vector4(.6f, .6f, .5f, 1);
        }
    }
}
